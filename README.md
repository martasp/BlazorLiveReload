# (Hot reload is suported in .net core out of the box, so this library is obsolete)

# BlazorLiveReload 
Blazor Live Reload without refreshing page
### Installing
1.Add package: 
```
dotnet add package RazorComponentsPreview --version 0.6.0
```
2.Add to startup: 
```
services.AddRazorComponentsRuntimeCompilation();
app.UseRazorComponentsRuntimeCompilation();
```
3.run project go to /preview and change razor file components

### Demo
![Alt Text](https://media.giphy.com/media/QVhHivBsXgSctpqt4s/giphy.gif)


