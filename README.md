# BlazorLiveReload
Blazor Live Reload without refreshing page
### Installing

1.Open cmd and copy this : 
```
git clone https://github.com/martasp/BlazorLiveReload.git
cd BlazorLiveReload
cd BlazorApp1
dotnet watch run
```
2.Open browser localhost:5000\
3.Change blazor code\
4.Wait for relaod

### Demo
![Alt Text](https://media.giphy.com/media/nwLXDA543oC9gCQJWA/giphy.gif)


### Adding to your project
Add to index.html file
```
    <script>
        var wait = ms => new Promise((r, j)=>setTimeout(r, ms))
        var failed = false;
        async function fetchAsync () {
        try {
             let res = await fetch('http://localhost:5000') // ping local server
             //if failed before and now sucess then reload page
             if(failed && res.status===200)
             {
                failed = false;
                location.reload();
             }
            }
          catch (e) 
          {
            failed = true;
            await wait(200);
            console.log("failed"+ e)
          }
        }
        async function Pooling () {
            while(true)
                {
                    fetchAsync();
                    await wait(200);
                }
        }
        Pooling();
       </script>
```
Add to "ProjectName".csproj file
```
  <ItemGroup>
    <Watch Include="**\*.cshtml" />
  </ItemGroup>
```
