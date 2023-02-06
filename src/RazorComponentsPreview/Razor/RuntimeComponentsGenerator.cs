using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace RazorComponentsPreview
{
    public class RuntimeComponentsGenerator
    {
        public IServiceCollection _serviceCollection { get; set; }
        public Generator _generator { get; set; }
        public string _fileSystemPath { get; set; } = Directory.GetCurrentDirectory();
        public WebSocket _webSocket { get; set; }
        public FileSystemWatcher _watcher { get; set; }
        public string _CurrentRoute { get; set; } = "/";
        public RuntimeComponentsGenerator(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
            _generator = new Generator();
            _watcher = new FileSystemWatcher();
            var razorFiles = GetAllFileNames();
            var files = ReadAllFiles(razorFiles);

            foreach (var file in files)
            {
                _generator.Add(file.FilePath, file.Content);
            }
        }

        public string FirstTimeRender()
        {
            try
            {
                var html = RenderRazorFileToHtml("index"); // Todo handle case : if there is not index.razor file in project
                var wrapedhtml = WrapHostTemplate(html);
                File.WriteAllText("wwwroot/preview.html", wrapedhtml);
                return wrapedhtml;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }
        public void AttachWebsocket(WebSocket webSocket)
        {
            _webSocket = webSocket;
        }
        private List<string> GetAllFileNames()
        {
            var razorFiles = Directory.GetFiles(_fileSystemPath, "*.razor", SearchOption.AllDirectories).ToList();
            var item1 = razorFiles.SingleOrDefault(item => item.Contains("_Imports.razor"));
            razorFiles.Remove(item1);
            return razorFiles;
        }
        private static List<(string FilePath, string Content)> ReadAllFiles(List<string> razorFiles)
        {
            var files = razorFiles.Select(item => (FilePath: "/" + Path.GetFileName(item), Content: File.ReadAllText(item))).ToList();
            var razorImportsFile = ("/_Imports.razor", @"
                                    @using System.Net.Http
                                    @using Microsoft.AspNetCore.Authorization
                                    @using Microsoft.AspNetCore.Components.Authorization
                                    @using Microsoft.AspNetCore.Components.Forms
                                    @using Microsoft.AspNetCore.Components.Routing
                                    @using Microsoft.AspNetCore.Components.Web
                                    @using Microsoft.JSInterop
                                    @using Test
                                    @namespace Test 
                                    "); // Todo handle better way _Imports.razor file.
            files.Insert(0, razorImportsFile);

            //hack for to get dependencies from Test
            var appRazorItem = files.SingleOrDefault(item => item.FilePath.Contains("App.razor"));

            if (appRazorItem != default)
            {
                var fixedAPPcontent = appRazorItem.Content.Replace("@typeof(", "@typeof(Test.").Replace("Program", "Counter"); //Todo change name "Counter" to dynamic type from Test Assemebly
                files.Remove(appRazorItem);
                files.Add((appRazorItem.FilePath, fixedAPPcontent));
            }

            return files;
        }
        private string UpdateAllComponents(Generator generator, List<(string FilePath, string Content)> files) //Todo refactor : update only one syntax tree.
        {
            var CsharpFiles = new List<String>();
            foreach (var file in files)
            {
                CsharpFiles.Add(generator.Update(file.FilePath, file.Content).GetCSharpDocument().GeneratedCode);
            }
            //slow
            //var CsharpFiles = files.Select(file => generator.Update(file.FilePath, file.Content).GetCSharpDocument().GeneratedCode).ToList();

            var SyntaxTrees = CsharpFiles.Select(file => CSharpSyntaxTree.ParseText(file, new CSharpParseOptions(LanguageVersion.Preview))).ToList();
            var references = generator.GetReferences;

            var componentInstance = Compile(SyntaxTrees, "Test" + Guid.NewGuid().ToString(), references); //Todo remove Test assebly allocations by unloading assembly
            var host = new TestHost(_serviceCollection);

            _serviceCollection.AddTransient<NavigationManager, HttpNavigationManager>();
            var httpNavigationManager = new HttpNavigationManager("https://localhost:5001/", $"https://localhost:5001/{_CurrentRoute}"); // Todo add currently updated component page route name.
            var jsRuntime = new UnsupportedJavaScriptRuntime();

            host.AddService<IJSRuntime, UnsupportedJavaScriptRuntime>(jsRuntime);
            host.AddService<NavigationManager, HttpNavigationManager>(httpNavigationManager);
            var component = host.AddComponent(componentInstance);
            var htmlString = component.GetMarkup();

            return htmlString;
        }
        private string GetWebsocketScriptCode()
        {
            return @"
            <script>
                // Create WebSocket connection.
                var scheme = 'wss';
                var port = document.location.port ? (':' + document.location.port) : '';
                var url = scheme + '://' + document.location.hostname + port + '/ws';
                var socket = new WebSocket(url);

                // Connection opened
                //socket.addEventListener('open', function(event) {
                //    socket.send('Hello Server!');
                //});

                // Listen for messages
                socket.addEventListener('message', function(event) {
                    console.log('Message from server ', event.data);
                    document.body.innerHTML = event.data;
                });
            </script>";
        }
        public void AddRazorStaticRuntimeGeneration()
        {
            // Create a new FileSystemWatcher and set its properties.
            _watcher.Path = _fileSystemPath;
            _watcher.IncludeSubdirectories = true;
            // Watch for changes in LastAccess and LastWrite times, and
            // the renaming of files or directories.
            _watcher.NotifyFilter = NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName;

            // Only watch text files.
            _watcher.Filter = "*.razor";
            //_watcher.Add("*.razor");
            // Add event handlers.
            //_watcher.Changed += OnChanged;
            //_watcher.Created += OnChanged;
            //_watcher.Deleted += OnChanged;
            _watcher.Renamed += OnChanged;

            // Begin watching.
            _watcher.EnableRaisingEvents = true;
        }
        private async void OnChanged(object source, FileSystemEventArgs e)
        {
            if (e.Name.Contains("TMP"))
            {
                return;
            }
            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
            try
            {
                changeRouteName(e.Name); //Todo handle routing better in other ways.
                var html = RenderRazorFileToHtml(e.Name/*.Split("~")[0]*/);
                var wrapedhtml = WrapHostTemplate(html);
                File.WriteAllText("wwwroot/preview.html", wrapedhtml);
                await SentHtmlToCLient(wrapedhtml);
                //Console.WriteLine(wrapedhtml);
            }
            catch (Exception ex)
            {
                await SentHtmlToCLient(ex.Message); //Todo make better error Formating and filter erros.
            }
        }

        private void changeRouteName(string fileName)
        {
            var filePath = Directory.GetFiles(_fileSystemPath, fileName, SearchOption.AllDirectories).ToList().FirstOrDefault(); //Todo: do file caching,async stuff if its slow
            var fileContent = File.ReadAllLines(filePath);
            var pageLine = fileContent.SingleOrDefault(line => line.Contains("@page"));
            var route = pageLine != null ? pageLine.Split(' ')[1].Split("\"")[1] : _CurrentRoute; // set new route if page component have route  otherwise set last route.
            _CurrentRoute = route;
        }
        public string Between(string STR, string FirstString, string LastString)
        {
            string FinalString;
            int Pos1 = STR.IndexOf(FirstString) + FirstString.Length;
            int Pos2 = STR.IndexOf(LastString);
            FinalString = STR.Substring(Pos1, Pos2 - Pos1);
            return FinalString;
        }
        private string WrapHostTemplate(string html) //Todo make cached templete if its slow.
        {
            var hostFile = Directory.GetFiles(_fileSystemPath, "_Host.cshtml", SearchOption.AllDirectories).ToList().FirstOrDefault();
            var hostFileContent = File.ReadAllText(hostFile);
            var script = GetWebsocketScriptCode();
            //var app = Between(hostFile, "<App>", "</App>");
            //var body = Between(hostFile, "<body>", "</body>");
            var head = Between(hostFileContent, "<head>", "</head>");
            head = head.Replace("href=\"", "href=\"/");
            return $"<!DOCTYPE html><html lang='en'><head>{head}</head><body><app>{html}</app>{script}</body></html>";
        }
        private async Task SentHtmlToCLient(string html)
        {
            if (_webSocket!=null) // _webSocket is attached only /preview route is hited
            {
                byte[] bytes = Encoding.ASCII.GetBytes(html);
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                Console.WriteLine("Not conectected to webSocket, go to /preivew page");
            }
        }
        private string RenderRazorFileToHtml(string name)
        {
            var razorFiles = GetAllFileNames();/*.Where(item => item.Contains(name)).ToList();*/
            var fileContent = ReadAllFiles(razorFiles);
            return UpdateAllComponents(_generator, fileContent);
        }
        public IComponent Compile(IEnumerable<SyntaxTree> syntaxTrees, string asseblyName, List<MetadataReference> references)
        {
            var compilation = CSharpCompilation.Create(asseblyName, syntaxTrees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using (MemoryStream stream = new MemoryStream())
            {
                EmitResult result = compilation.Emit(stream);

                if (!result.Success)
                {
                    var errors = string.Join(Environment.NewLine, result.Diagnostics);
                    Console.WriteLine(errors); //Todo return errors to html if compilation is not success
                    throw new Exception(errors);
                }

                stream.Seek(0, SeekOrigin.Begin);

                var context = AssemblyLoadContext.Default;
                var asm = context.LoadFromStream(stream);

                try
                {
                    var type = asm.GetExportedTypes().SingleOrDefault(item => item.Name == "App");  //TODO make more posibilities returning not only APP component, because somtimes its harder to setup routes in runtime.
                    var instance = (IComponent)Activator.CreateInstance(type);
                    return instance;
                }
                catch (Exception)
                {
                    throw new Exception(String.Join(Environment.NewLine,result.Diagnostics));
                }

            }
        }
    }
}
