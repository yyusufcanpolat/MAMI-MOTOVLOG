using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

// Executes the real MVC app in an isolated temporary directory and SQLite database.
// No SMTP, live SQL, live domain, or external route API calls. The geometry is a test fixture.
if (args.Length != 1) throw new ArgumentException("Provide the compiled MamiMotovlog.dll path.");
var dll = Path.GetFullPath(args[0]);
if (!File.Exists(dll)) throw new FileNotFoundException("Build Release first", dll);
var temp = Path.Combine(Path.GetTempPath(), "mami-v53-test-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temp);
Directory.CreateDirectory(Path.Combine(temp,"wwwroot"));
var listener = new TcpListener(IPAddress.Loopback,0); listener.Start();
var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
var address = $"http://127.0.0.1:{port}";
var adminPassword = "Local-Test!Admin-53";
var password = "Local-Test!Rider-53";
var newPassword = "Local-Test!Rider-54";
var logs = new ConcurrentQueue<string>();
var start = new ProcessStartInfo("dotnet") { WorkingDirectory=temp, RedirectStandardOutput=true, RedirectStandardError=true, UseShellExecute=false, CreateNoWindow=true };
start.ArgumentList.Add(dll); start.ArgumentList.Add("--urls"); start.ArgumentList.Add(address);
start.Environment["ASPNETCORE_ENVIRONMENT"]="Development";
start.Environment["DOTNET_ENVIRONMENT"]="Development";
start.Environment["ASPNETCORE_CONTENTROOT"]=temp;
start.Environment["Database__Provider"]="Sqlite";
start.Environment["ConnectionStrings__Sqlite"]="Data Source="+Path.Combine(temp,"App_Data","site.db");
start.Environment["ConnectionStrings__SqlServer"]="";
start.Environment["Rider__Enabled"]="true";
start.Environment["Admin__Password"]=adminPassword;
start.Environment["Mail__LocalConsoleOnly"]="true";
start.Environment["Mail__SendReminders"]="false";
start.Environment["DataProtection__KeysDirectory"]=Path.Combine(temp,"keys");
start.Environment["Logging__LogLevel__Default"]="Warning";
start.Environment["Logging__LogLevel__Microsoft.EntityFrameworkCore"]="Warning";
using var proc=new Process{StartInfo=start};
proc.OutputDataReceived += (_,e)=>{if(e.Data!=null)logs.Enqueue(e.Data);};
proc.ErrorDataReceived += (_,e)=>{if(e.Data!=null)logs.Enqueue(e.Data);};
proc.Start();proc.BeginOutputReadLine();proc.BeginErrorReadLine();
HttpClient Client()=>new(new HttpClientHandler{CookieContainer=new CookieContainer(),AllowAutoRedirect=false}){BaseAddress=new Uri(address),Timeout=TimeSpan.FromSeconds(15)};
using var a=Client();using var b=Client();
void Check(bool success,string label) {if(!success)throw new Exception("FAIL: "+label);Console.WriteLine("PASS: "+label);}
async Task<string> Get(HttpClient c,string path,int expected=200){using var r=await c.GetAsync(path);Check((int)r.StatusCode==expected,$"GET {path} -> {expected} (actual {(int)r.StatusCode})");return await r.Content.ReadAsStringAsync();}
string Token(string html){foreach(Match m in Regex.Matches(html,"<input\\b[^>]*>",RegexOptions.IgnoreCase)){if(!m.Value.Contains("__RequestVerificationToken"))continue;var v=Regex.Match(m.Value,"value=\"([^\"]+)\"");if(v.Success)return WebUtility.HtmlDecode(v.Groups[1].Value);}throw new Exception("No CSRF token on GET");}
async Task<HttpResponseMessage> Post(HttpClient c,string path,string? csrf,Dictionary<string,string> values,bool ajax=false){if(csrf!=null)values["__RequestVerificationToken"]=csrf;using var req=new HttpRequestMessage(HttpMethod.Post,path);if(ajax){var form=new MultipartFormDataContent();foreach(var pair in values)form.Add(new StringContent(pair.Value),pair.Key);req.Content=form;}else req.Content=new FormUrlEncodedContent(values);if(ajax){req.Headers.Add("X-Mami-Async","1");req.Headers.Accept.ParseAdd("application/json");}return await c.SendAsync(req);}
async Task<string> Redirect(HttpResponseMessage r,string label){using(r){var body=await r.Content.ReadAsStringAsync();Check((int)r.StatusCode is 302 or 303,label+" -> redirect (actual "+(int)r.StatusCode+")");return r.Headers.Location?.ToString()??throw new Exception("Missing location");}}
async Task<string> MailLink(string path,int oldCount){for(int i=0;i<50;i++){var match=logs.ToArray().Skip(oldCount).Select(l=>Regex.Match(l,Regex.Escape(address+path)+@"[^\s<>]+" )).FirstOrDefault(m=>m.Success);if(match!=null)return match.Value;await Task.Delay(100);}throw new Exception("Development email link not found: "+path);}
async Task Register(HttpClient c,string name,string email){var t=Token(await Get(c,"/uye/kayit"));var before=logs.Count;await Redirect(await Post(c,"/uye/kayit",t,new(){["Name"]=name,["Email"]=email,["Password"]=password,["Confirm"]=password}),"registration");var link=await MailLink("/uye/dogrula",before);using var verify=await c.GetAsync(link);Check((int)verify.StatusCode==302,"email verification redirect");}
async Task Login(HttpClient c,string email,string pass){var t=Token(await Get(c,"/uye/giris"));var target=await Redirect(await Post(c,"/uye/giris",t,new(){["Email"]=email,["Password"]=pass}),"login");Check(target=="/garaj","rider login redirects to garage");}
try{
    bool ready=false;for(int i=0;i<100;i++){if(proc.HasExited)break;try{using var response=await a.GetAsync("/");if(response.IsSuccessStatusCode){ready=true;break;}}catch(HttpRequestException){}await Task.Delay(300);}Check(ready,"real application started, isolated SQLite");
    for(int i=0;i<16;i++){using var response=await a.GetAsync("/uye/giris");if(!response.IsSuccessStatusCode)throw new Exception("GET rate limit regression: "+response.StatusCode);}Check(true,"16 login GETs do not consume POST limit");
    await Register(a,"Test Rider A","a@example.test");await Login(a,"a@example.test",password);
    // Same browser, both cookies present: admin and rider requests must bind to different principals.
    var admin=Token(await Get(a,"/yonetim/giris"));await Redirect(await Post(a,"/yonetim/giris",admin,new(){["Password"]=adminPassword}),"admin login");
    await Get(a,"/yonetim/rider/uyeler");
    foreach(var publicPath in new[]{"/","/hakkimda","/motosikletim","/rehber","/ekipmanlarim","/videolar","/rotalar","/galeri","/iletisim","/sitemap.xml"}) await Get(a,publicPath);
    var contentToken=Token(await Get(a,"/yonetim/ayarlar"));
    await Redirect(await Post(a,"/yonetim/ayarlar",contentToken,new(){["MotorcycleTitle"]="Integration motorcycle",["MotorcycleText"]="Motorcycle custom fixture",["GuideTitle"]="Integration guide",["GuideText"]="Guide custom fixture"}),"admin page content update");
    Check((await Get(a,"/motosikletim")).Contains("Motorcycle custom fixture"),"motorcycle text persisted");
    Check((await Get(a,"/rehber")).Contains("Guide custom fixture"),"guide text persisted");
    var add=Token(await Get(a,"/garaj/motor/ekle"));
    using(var invalid=await Post(a,"/garaj/motor/ekle",add,new(){["BrandName"]="",["ModelName"]="",["ModelYear"]="2024",["OdometerKm"]="1000"})){Check((int)invalid.StatusCode==200,"required fields redisplay form, not 405");}
    var bikePath=await Redirect(await Post(a,"/garaj/motor/ekle",add,new(){["BrandName"]="CFMOTO",["ModelName"]="450SR",["ModelYear"]="2024",["OdometerKm"]="1000",["Edition"]="",["Notes"]="",["AverageFuelL100"]="4.5",["TankLitres"]=""}),"optional bike fields");
    var bikeId=bikePath.Split('/').Last();Check(int.TryParse(bikeId,out _),"bike ID created");
    var date=DateTime.UtcNow.AddHours(3).Date.ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
    foreach(var (km,litres,price) in new[]{("1000","10","500"),("1200","8","400")}){
        var token=Token(await Get(a,bikePath+"/yakit"));await Redirect(await Post(a,"/garaj/yakit",token,new(){["BikeId"]=bikeId,["Date"]=date,["OdometerKm"]=km,["Litres"]=litres,["AmountTry"]=price,["FullTank"]="true"}),"fuel record "+km);}
    var duplicateToken=Token(await Get(a,bikePath+"/yakit"));
    using(var duplicate=await Post(a,"/garaj/yakit",duplicateToken,new(){["BikeId"]=bikeId,["Date"]=date,["OdometerKm"]="1200",["Litres"]="8",["AmountTry"]="400",["FullTank"]="true"})) Check((int)duplicate.StatusCode==200,"duplicate fuel rejected without server error");
    var details=await Get(a,bikePath);Check(details.Contains("4.00")&&details.Contains("900"),"measured fuel 4.00 and total 900");
    var maintenance=Token(await Get(a,bikePath+"/bakim"));await Redirect(await Post(a,"/garaj/bakim",maintenance,new(){["BikeId"]=bikeId,["Category"]="Yağ değişimi",["Date"]=date,["OdometerKm"]="1200",["CostTry"]="",["Description"]="",["NextDate"]="",["NextKm"]=""}),"optional maintenance fields");
    var expense=Token(await Get(a,bikePath+"/gider"));await Redirect(await Post(a,"/garaj/gider",expense,new(){["BikeId"]=bikeId,["Date"]=date,["Category"]="Diğer",["Description"]="Test gider",["AmountTry"]="150.25"}),"expense record");
    var gear=Token(await Get(a,"/garaj/ekipman-ekle"));await Redirect(await Post(a,"/garaj/ekipman-ekle",gear,new(){["Category"]="Kask",["Brand"]="Test",["Model"]="Ekipman",["CostTry"]="",["PurchasedOn"]="",["WarrantyEndsOn"]=""}),"optional gear fields");
    var planner=Token(await Get(a,"/rota-planlayici"));
    Dictionary<string,string> TripData()=>new(){["Title"]="Test route fixture",["StartLabel"]="",["EndLabel"]="",["StartLat"]="41",["StartLon"]="29",["EndLat"]="41.2",["EndLon"]="29.5",["DistanceKm"]="60.25",["DurationMinutes"]="65",["BikeId"]=bikeId,["GeoJson"]="{\"type\":\"LineString\",\"coordinates\":[[29,41],[29.5,41.2]]}"};
    using(var invalid=await Post(a,"/rota-planlayici/kaydet",null,TripData(),true)){Check((int)invalid.StatusCode==400,"missing anti-CSRF token rejected");Check((await invalid.Content.ReadAsStringAsync()).Contains("error"),"AJAX 400 structured message");}
    using(var malformed=await Post(a,"/rota-planlayici/kaydet",planner,new(){["Title"]="Invalid"},true)) Check((int)malformed.StatusCode==400,"missing route coordinates rejected");
    string tripPath;using(var saved=await Post(a,"/rota-planlayici/kaydet",planner,TripData(),true)){var body=await saved.Content.ReadAsStringAsync();Check(saved.IsSuccessStatusCode,"route AJAX save (both cookies) -> "+saved.StatusCode);using var json=JsonDocument.Parse(body);tripPath=json.RootElement.GetProperty("url").GetString()!;}
    Check(tripPath.StartsWith("/garaj/rotalar/"),"route URL returned");var trip=await Get(a,tripPath);Check(trip.Contains("Test route fixture",StringComparison.OrdinalIgnoreCase),"route persisted");
    await Register(b,"Test Rider B","b@example.test");await Login(b,"b@example.test",password);await Get(b,bikePath,404);await Get(b,tripPath,404);var bToken=Token(await Get(b,"/garaj"));using(var forbidden=await Post(b,bikePath+"/sil",bToken,new()))Check((int)forbidden.StatusCode==404,"cannot delete another user's bike");
    using(var forbidden=await b.GetAsync("/yonetim/rider/uyeler"))Check((int)forbidden.StatusCode==302,"rider cannot enter admin");
    var share=Token(await Get(a,tripPath));await Redirect(await Post(a,tripPath+"/paylas",share,new()),"request route publication");
    var routeId=tripPath.Split('/').Last();var moderation=Token(await Get(a,"/yonetim/rider"));await Redirect(await Post(a,"/yonetim/rider/rota-onay/"+routeId,moderation,new(){["approved"]="true"}),"admin approves route");
    var community=Token(await Get(b,"/topluluk/rotalar"));await Redirect(await Post(b,"/topluluk/rotalar/"+routeId+"/favori",community,new()),"community GET/POST same user CSRF");
    var logout=Token(await Get(a,"/garaj"));await Redirect(await Post(a,"/uye/cikis",logout,new()),"rider logout");await Get(a,"/yonetim/rider/uyeler");
    var forgot=Token(await Get(a,"/uye/sifremi-unuttum"));var n=logs.Count;await Redirect(await Post(a,"/uye/sifremi-unuttum",forgot,new(){["email"]="a@example.test"}),"password reset requested");var resetLink=await MailLink("/uye/sifre-yenile",n);var resetToken=Token(await Get(a,resetLink));var code=Uri.UnescapeDataString(Regex.Match(new Uri(resetLink).Query,"[?&]code=([^&]+)").Groups[1].Value);await Redirect(await Post(a,"/uye/sifre-yenile",resetToken,new(){["Email"]="a@example.test",["Code"]=code,["Password"]=newPassword}),"password changed");await Login(a,"a@example.test",newPassword);
    var del=Token(await Get(a,"/yonetim/rider/motosikletler"));await Redirect(await Post(a,"/yonetim/rider/motor/"+bikeId+"/sil",del,new()),"admin deletes bike");await Get(a,bikePath,404);await Get(a,tripPath);
    var profile=Token(await Get(a,"/uye/profil"));await Redirect(await Post(a,"/uye/hesabi-sil",profile,new(){["password"]=newPassword}),"delete account and owned rows");await Get(b,"/topluluk/rotalar/"+routeId,404);await Get(b,"/garaj");
    Console.WriteLine("PASS: all isolated MVC HTTP scenarios. External API/SQL Server/SMTP not tested.");
}catch{Console.Error.WriteLine("Test stopped. Review local app errors; development email tokens omitted.");foreach(var line in logs.Where(x=>!x.Contains("http")&&!x.Contains("Mail to")).TakeLast(25))Console.Error.WriteLine(line);throw;}
finally{if(!proc.HasExited){proc.Kill(entireProcessTree:true);await proc.WaitForExitAsync();}try{Directory.Delete(temp,true);}catch(IOException){Console.WriteLine("Temporary local test directory: "+temp);}}
