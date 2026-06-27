using DustInTheWind.PeerBerry.Toolkit;

namespace P2PReport.Adapters;

public class PeerBerryAdapter : IPlatformAdapter
{
  public string Name => "peerberry";

  public async Task<IReadOnlyList<Transaction>> LoadTransactionsAsync(string[] files)
  {
    var tasks = files
        .Select(file => TransactionsDocument.LoadFromFileAsync(file))
        .ToArray();

    var documents = await Task.WhenAll(tasks);

    return documents
        .SelectMany(doc => doc.TransactionsSection.Transactions)
        .Select(r => new Transaction(
            DateOnly.FromDateTime(r.Date),
            r.Amount,
            GetCategory(r.Type)))
        .ToList();
  }

  private static string GetCategory(TransactionType type)
  {
    if (type == TransactionType.Investment)
      return "Internal";
    if (type == TransactionType.RepaymentInterest || type == TransactionType.BuybackInterest)
      return "Gain";
    if (type == TransactionType.RepaymentPrincipal || type == TransactionType.BuybackPrincipal)
      return "Internal";
    if (type == TransactionType.Withdrawal)
      return "Withdrawal";

    return "Internal";
  }
}
