namespace HumanOS.SimulationLabs.Api.Features.LabVersions;

/// <summary>
/// Authorization policies for LabVersion operations.
/// </summary>
public static class LabVersionPolicies
{
    public const string Read = "LabVersions.Read";
    public const string Create = "LabVersions.Create";
    public const string Update = "LabVersions.Update";
    public const string Approve = "LabVersions.Approve";
    public const string Publish = "LabVersions.Publish";
    public const string Retire = "LabVersions.Retire";
    public const string Manage = "LabVersions.Manage";
}
