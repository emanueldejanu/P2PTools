using DustInTheWind.Fintown.Toolkit;

namespace P2PReport.Adapters;

public class FintownAdapter : IPlatformAdapter
{
  public string Name => "fintown";

  public async Task<IReadOnlyList<Transaction>> LoadTransactionsAsync(string[] files)
  {
    var tasks = files
        .Select(file => TransactionsDocument.LoadFromFileAsync(file))
        .ToArray();

    var documents = await Task.WhenAll(tasks);

    return documents
        .SelectMany(doc => doc)
        .Select(r => new Transaction(
            DateOnly.FromDateTime(r.Date),
            r.Amount.Value,
            GetCategory(r.Description)))
        .ToList();
  }

  private static string GetCategory(TransactionDescription description)
  {
    if (description == TransactionDescription.DepositFunds)
      return "Deposit";
    if (description == TransactionDescription.InterestPayOut)
      return "Gain";
    if (description == TransactionDescription.InvestingFunds)
      return "Internal";

    return "Internal";
  }
}
