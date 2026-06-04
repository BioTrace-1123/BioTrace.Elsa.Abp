using Volo.Abp.Reflection;

namespace BioTrace.Elsa.Abp.Permissions;

public static class AbpElsaPermissions
{
    public const string GroupName = "Abp.Elsa";

    /// <summary>Grants all Elsa API permissions (<see cref="ElsaApiPermissionNames.Wildcard"/>).</summary>
    public const string Admin = GroupName + ".Admin";

    /// <summary>Bypass Elsa read-only workflow definition restrictions.</summary>
    public const string NotReadOnly = GroupName + ".NotReadOnly";

    public static class WorkflowDefinitions
    {
        public const string Default = GroupName + ".WorkflowDefinitions";
        public const string Read = Default + ".Read";
        public const string Write = Default + ".Write";
        public const string Publish = Default + ".Publish";
        public const string Retract = Default + ".Retract";
        public const string Delete = Default + ".Delete";
    }

    public static class WorkflowInstances
    {
        public const string Default = GroupName + ".WorkflowInstances";
        public const string Read = Default + ".Read";
        public const string Write = Default + ".Write";
        public const string Delete = Default + ".Delete";
        public const string Cancel = Default + ".Cancel";
        public const string Execute = Default + ".Execute";
    }

    public static class Alterations
    {
        public const string Default = GroupName + ".Alterations";
        public const string Read = Default + ".Read";
        public const string Write = Default + ".Write";
    }

    public static class Tasks
    {
        public const string Default = GroupName + ".Tasks";
        public const string Complete = Default + ".Complete";
    }

    public static class HttpEndpoints
    {
        public const string Default = GroupName + ".HttpEndpoints";
        public const string Invoke = Default + ".Invoke";
    }

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(AbpElsaPermissions));
    }
}
