using DustInTheWind.Mintos.Toolkit;

var dictCategories = new Dictionary<string, string>()
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
  ["Tax withholding - Bonds"] = "Tax",
  ["Withdrawal"] = "Withdrawal",
  ["Withholding tax"] = "Tax",
};

var tasks = Directory
    .GetFiles(@"C:\Personal\Mintos", "*.csv")
    .Select(filePath => StatementDocument.LoadFromFileAsync(filePath))
    .ToArray();

var documents = await Task.WhenAll(tasks);

foreach (var grp in documents.SelectMany(doc => doc).GroupBy(GetCategory))
{
  Console.WriteLine("{0,-56}: {1,10:0.00}", grp.Key, grp.Sum(x => x.Turnover));
}

string GetCategory(TransactionRecord record)
{
  var mintosLabel = record.PaymentType.GetLabel();
  return dictCategories[mintosLabel];
}
