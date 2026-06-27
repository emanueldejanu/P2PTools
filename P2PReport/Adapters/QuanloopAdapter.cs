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
            GetCategory(r.Description, r.Amount)))
        .ToList();
  }

  private static string GetCategory(string description, decimal amount)
  {
    var lower = description.ToLowerInvariant();

    if (lower.Contains("interest"))
      return "Gain";
    if (lower.Contains("deposit") || lower.Contains("top up"))
      return "Deposit";
    if (lower.Contains("withdrawal"))
      return "Withdrawal";
    if (lower.Contains("fee"))
      return "Fee";
    if (lower.Contains("tax"))
      return "Tax";

    return "Internal";
  }
}
