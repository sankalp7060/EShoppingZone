namespace EShoppingZone.Profile.Domain.ValueObjects
{
    public record EmailValue
    {
        public string Value { get; }

        public EmailValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email cannot be empty");

            if (!IsValidEmail(value))
                throw new ArgumentException("Invalid email format");

            Value = value.ToLower();
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public override string ToString() => Value;
    }
}
