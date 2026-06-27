using System.CommandLine;
using P2PReport.Adapters;

IPlatformAdapter[] adapters =
[
  new MintosAdapter(),
  new PeerBerryAdapter(),
  new FintownAdapter(),
  new QuanloopAdapter(),
];

var pathArgument = new Argument<string>("path")
{
  Description = "Path to a CSV file or a directory containing CSV files"
};

var outputOption = new Option<FileInfo?>("--output", "-o")
{
  Description = "Output CSV file path (defaults to console)"
};

var platformOption = new Option<string>("--platform", "-p")
{
  Description = "Platform name (mintos, peerberry, fintown, quanloop)"
};
platformOption.AcceptOnlyFromAmong([.. adapters.Select(a => a.Name)]);

var csvCommand = new Command("csv", "Export monthly category report as CSV");
csvCommand.Arguments.Add(pathArgument);
csvCommand.Options.Add(outputOption);
csvCommand.Options.Add(platformOption);

csvCommand.SetAction(async (parseResult, cancellationToken) =>
{
  var path = parseResult.GetValue(pathArgument)!;
  var outputFile = parseResult.GetValue(outputOption);
  var platformName = parseResult.GetValue(platformOption)!;

  var adapter = adapters.First(a => a.Name == platformName);
  var csvFiles = GetCsvFiles(path);

  var stopWatch = System.Diagnostics.Stopwatch.StartNew();
  var transactions = await adapter.LoadTransactionsAsync(csvFiles);
  stopWatch.Stop();

  Console.Error.WriteLine($"Loaded {transactions.Count} transactions in {stopWatch.ElapsedMilliseconds} ms");

  string[] categories = ["Deposit", "Withdrawal", "Gain", "Fee", "Tax", "Internal"];

  var monthlyData = transactions
      .GroupBy(r => (Month: new DateOnly(r.Date.Year, r.Date.Month, 1), r.Category))
      .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

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

var rootCommand = new RootCommand("P2P Report - monthly category report for P2P platforms");
rootCommand.Subcommands.Add(csvCommand);

return rootCommand.Parse(args).Invoke();

static string[] GetCsvFiles(string path)
{
  if (File.Exists(path))
    return [path];

  if (Directory.Exists(path))
    return Directory.GetFiles(path, "*.csv");

  throw new FileNotFoundException($"Path not found: {path}");
}
