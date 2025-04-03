namespace rps.Models
{
    public class TranscriptApplication
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? MatNo { get; set; }
        public string? Email { get; set; }
        public string? PhoneNo { get; set; }
        public TranscriptStatus Status { get; set; } = TranscriptStatus.Pending; // Default to Pending
        public DateTime DateApplied { get; set; } = DateTime.UtcNow; // Default to current date
        public string? Program { get; set; }
        public string? DestinationName { get; set; }
        public string? DestinationEmail { get; set; }
        public string? Note { get; set; } // Comments
        public string? TransactionId { get; set; } // Payment Ref
        public TranscriptType Type { get; set; } = TranscriptType.Student; // Default to Student
    }

    public enum TranscriptStatus
    {
        Pending,
        Processing,
        Approved,
        Rejected
    }

    public enum TranscriptType
    {
        Student,
        Schorlaship,
        Transfer,
        PG,
        Graduate
    }
}