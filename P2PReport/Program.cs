using System.CommandLine;
using DustInTheWind.Mintos.Toolkit;

var pathArgument = new Argument<string>("path")
{
  Description = "Path to a CSV file or a directory containing CSV files"
};

var outputOption = new Option<FileInfo?>("--output", "-o")
{
  Description = "Output CSV file path (defaults to console)"
};

var rootCommand = new RootCommand("P2P Report - Mintos monthly category report");
rootCommand.Arguments.Add(pathArgument);
rootCommand.Options.Add(outputOption);

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
  var path = parseResult.GetValue(pathArgument)!;
  var outputFile = parseResult.GetValue(outputOption);

  var csvFiles = GetCsvFiles(path);

  var stopWatch = System.Diagnostics.Stopwatch.StartNew();
  var tasks = csvFiles
      .Select(filePath => StatementDocument.LoadFromFileAsync(filePath))
      .ToArray();

  var documents = await Task.WhenAll(tasks);
  stopWatch.Stop();

  Console.Error.WriteLine($"Loaded {documents.Length} documents in {stopWatch.ElapsedMilliseconds} ms");

  var records = documents.SelectMany(doc => doc).ToList();

  string[] categories = ["Deposit", "Withdrawal", "Gain", "Fee", "Tax", "Internal"];

  var monthlyData = records
      .GroupBy(r => (Month: new DateOnly(r.Date.Year, r.Date.Month, 1), Category: GetCategory(r)))
      .ToDictionary(g => g.Key, g => g.Sum(r => r.Turnover));

  var months = monthlyData.Keys
      .Select(k => k.Month)
      .Distinct()
      .OrderBy(m => m)
      .ToList();

  TextWriter writer = outputFile is not null
      ? new StreamWriter(outputFile.FullName)
      : Console.Out;

  try
  {
    writer.WriteLine("Month," + string.Join(",", categories) + ",Total");

    foreach (var month in months)
    {
      var values = categories
          .Select(cat => monthlyData.GetValueOrDefault((month, cat)))
          .ToList();

      var total = values.Sum();
      var formatted = values.Select(v => v.ToString("+0.00;-0.00;0.00"));

      writer.WriteLine(
          $"{month:yyyy-MM},{string.Join(",", formatted)},{total.ToString("+0.00;-0.00;0.00")}");
    }
  }
  finally
  {
    if (outputFile is not null)
      await writer.DisposeAsync();
  }
});

return rootCommand.Parse(args).Invoke();

static string[] GetCsvFiles(string path)
{
  if (File.Exists(path))
    return [path];

  if (Directory.Exists(path))
    return Directory.GetFiles(path, "*.csv");

  throw new FileNotFoundException($"Path not found: {path}");
}

static string GetCategory(TransactionRecord record)
{
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

  var mintosLabel = record.PaymentType.GetLabel();
  return dictCategories[mintosLabel];
}
