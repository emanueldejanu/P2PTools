using DustInTheWind.Quanloop.Toolkit;

namespace P2PReport.Adapters;

public class QuanloopAdapter : IPlatformAdapter
{
  public string Name => "quanloop";

  public async Task<IReadOnlyList<Transaction>> LoadTransactionsAsync(string[] files)
  {
    var tasks = files
        .Select(file => StatementDocument.LoadFromFileAsync(file))
        .ToArray();

    var documents = await Task.WhenAll(tasks);

    return documents
        .SelectMany(doc => doc)
        .Select(r => new Transaction(
            r.Date,
            r.Amount,
            GetCategory(r)))
        .ToList();
  }

  private static string GetCategory(TransactionRecord record)
  {
    var account = record.Account?.ToLowerInvariant() ?? string.Empty;
    var lower = record.Description.ToLowerInvariant();

    if (lower.Contains("interest") || lower.Contains("cashback") || (lower.Contains("referral") && account.Length == 0))
      return "Gain";
    if (lower.Contains("deposit") || lower.Contains("top up"))
      return "Deposit";
    if (lower.Contains("withdrawal") || (lower.Contains("referral") && account.Length > 0))
      return "Withdrawal";
    if (lower.Contains("fee"))
      return "Fee";
    if (lower.Contains("tax"))
      return "Tax";

    if (account.Length > 0 && !record.Counterpart.Equals("quanloop", StringComparison.OrdinalIgnoreCase))
      return "Deposit";

    throw new InvalidDataException("Unknown transaction category: " + record.Description);
  }
}
