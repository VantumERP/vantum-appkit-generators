using Vantum.AppKit;

namespace TestConsumer.Modules
{
    [AppModule(Name = "ContactsManagement",
               DisplayName = "Contacts",
               Version = "1.0.0",
               Description = "Manage customer contacts and relationships")]
    [AppPermissions("Contacts.Read", "Contacts.Write", "Contacts.Delete")]
    public class ContactsModule
    {
        [AppSetting("Contacts.DefaultGroup", AppSettingType.String,
                    DefaultValue = "General",
                    Description = "Default group for new contacts")]
        public const string DefaultGroupSetting = "Contacts.DefaultGroup";

        [AppSetting("Contacts.MaxPerPage", AppSettingType.Int,
                    DefaultValue = "50",
                    Description = "Maximum contacts to return per page")]
        public const string MaxPerPageSetting = "Contacts.MaxPerPage";

        [AppRoute("GET", "/api/contacts", RequiredPermission = "Contacts.Read")]
        public void GetContacts() { }

        [AppRoute("GET", "/api/contacts/{id}", RequiredPermission = "Contacts.Read")]
        public void GetContact() { }

        [AppRoute("POST", "/api/contacts", RequiredPermission = "Contacts.Write")]
        public void CreateContact() { }

        [AppRoute("PUT", "/api/contacts/{id}", RequiredPermission = "Contacts.Write")]
        public void UpdateContact() { }

        [AppRoute("DELETE", "/api/contacts/{id}", RequiredPermission = "Contacts.Delete")]
        public void DeleteContact() { }
    }

    [AppModule(Name = "TaskManagement",
               DisplayName = "Tasks",
               Version = "0.5.0",
               Description = "Task tracking and assignment")]
    [AppPermissions("Tasks.Read", "Tasks.Write", "Tasks.Assign")]
    public class TaskModule
    {
        [AppSetting("Tasks.DefaultPriority", AppSettingType.String,
                    DefaultValue = "Medium",
                    Description = "Default priority for new tasks")]
        private static readonly string DefaultPriority = "Tasks.DefaultPriority";

        [AppRoute("GET", "/api/tasks", RequiredPermission = "Tasks.Read")]
        public void GetTasks() { }

        [AppRoute("POST", "/api/tasks", RequiredPermission = "Tasks.Write")]
        public void CreateTask() { }

        [AppRoute("POST", "/api/tasks/{id}/assign", RequiredPermission = "Tasks.Assign")]
        public void AssignTask() { }
    }
}
