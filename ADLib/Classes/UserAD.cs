namespace ADLib.Classes
{
    internal class UserAD
    {
        public string DisplayName { get; }

        public string Id { get; }

        public UserAD(string displayName, string id)
        {
            DisplayName = displayName;
            Id = id;
        }
    }
}
