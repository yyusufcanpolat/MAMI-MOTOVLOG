using MamiMotovlog.Models;
namespace MamiMotovlog.ViewModels;
public sealed class AdminPlatformVm
{
    public string Section { get; init; }="";
    public string Query { get; init; }="";
    public int Page { get; init; }=1;
    public int Total { get; init; }
    public List<RiderMember> Members { get; init; }=[];
    public List<RiderBike> Bikes { get; init; }=[];
    public List<RiderTrip> Trips { get; init; }=[];
    public List<AdminAudit> Audits { get; init; }=[];
    public Dictionary<string,string> Names { get; init; }=[];
}
