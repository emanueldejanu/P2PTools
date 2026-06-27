namespace P2PReport.Adapters;

public record Transaction(DateOnly Date, decimal Amount, string Category);
