using DustInTheWind.Mintos.Toolkit;

namespace P2PReport.Adapters;

public class MintosAdapter : IPlatformAdapter
{
  private static readonly Dictionary<string, string> Categories = new()
  {
    ["Bond investment principal increase"] = "Internal",
    ["Bonus"] = "Gain",
    ["Deposits"] = "Deposit",
    ["Interest earned on overdue payments"] = "Gain",
    ["Interest received"] = "Gain",
    ["Interest received - Bonds"] = "Gain",
    ["Interest received from loan repurchase"] = "Gain",
    ["Investment"] = "Internal",
    ["Late fees received"] = "Gain",
    ["Loan Portfolios fee"] = "Fee",
    ["Notes cashout from Mintos strategies"] = "Internal",
    ["Principal received"] = "Internal",
    ["Principal received from loan repurchase"] = "Internal",
    ["Principal received from repurchase of small loan parts"] = "Internal",
    ["Real estate interest income"] = "Gain",
    ["Real estate investment principal increase"] = "Internal",
    ["Real estate tax withholding"] = "Tax",
    ["Secondary market transaction"] = "Internal",
    ["Secondary market transaction - discount or premium"] = "Internal",
    ["Tax withholding - Bonds"] = "Tax",
    ["Withdrawal"] = "Withdrawal",
    ["Withholding tax"] = "Tax",
  };

  public string Name => "mintos";

  public async Task<IReadOnlyList<Transaction>> LoadTransactionsAsync(string[] files)
  {
    var tasks = files
        .Select(file => StatementDocument.LoadFromFileAsync(file))
        .ToArray();

    var documents = await Task.WhenAll(tasks);

    return documents
        .SelectMany(doc => doc)
        .Select(r => new Transaction(
            DateOnly.FromDateTime(r.Date),
            r.Turnover,
            Categories[r.PaymentType.GetLabel()]))
        .ToList();
  }
}
