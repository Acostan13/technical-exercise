namespace StargateAPI.Business.Rules
{
    public static class DutyTitles
    {
        /// <summary>
        /// Rule: a Person is classified as 'Retired' when a Duty Title is 'RETIRED'.
        /// </summary>
        public const string Retired = "RETIRED";

        public static bool IsRetired(string? dutyTitle) =>
            string.Equals(dutyTitle?.Trim(), Retired, StringComparison.OrdinalIgnoreCase);
    }
}
