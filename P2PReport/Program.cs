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

var groupByOption = new Option<string>("--group-by", "-g")
{
  Description = "Group by period: day, month, or year",
  DefaultValueFactory = _ => "month"
};
groupByOption.AcceptOnlyFromAmong("day", "month", "year");

var csvCommand = new Command("csv", "Export category report as CSV");
csvCommand.Arguments.Add(pathArgument);
csvCommand.Options.Add(outputOption);
csvCommand.Options.Add(platformOption);
csvCommand.Options.Add(groupByOption);

csvCommand.SetAction(async (parseResult, cancellationToken) =>
{
  var path = parseResult.GetValue(pathArgument)!;
  var outputFile = parseResult.GetValue(outputOption);
  var platformName = parseResult.GetValue(platformOption)!;
  var groupBy = parseResult.GetValue(groupByOption)!;

  var adapter = adapters.First(a => a.Name == platformName);
  var csvFiles = GetCsvFiles(path);

  var stopWatch = System.Diagnostics.Stopwatch.StartNew();
  var transactions = await adapter.LoadTransactionsAsync(csvFiles);
  stopWatch.Stop();

  Console.Error.WriteLine($"Loaded {transactions.Count} transactions in {stopWatch.ElapsedMilliseconds} ms");

  string[] categories = ["Deposit", "Withdrawal", "Gain", "Fee", "Tax", "Internal"];

  var periodData = transactions
      .GroupBy(r => (Period: GetPeriod(r.Date, groupBy), r.Category))
      .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

  var periods = periodData.Keys
      .Select(k => k.Period)
      .Distinct()
      .OrderBy(p => p)
      .ToList();

  string dateFormat = groupBy switch
  {
    "day" => "yyyy-MM-dd",
    "year" => "yyyy",
    _ => "yyyy-MM"
  };

  TextWriter writer = outputFile is not null
      ? new StreamWriter(outputFile.FullName)
      : Console.Out;

  try
  {
    writer.WriteLine("Period," + string.Join(",", categories));

    foreach (var period in periods)
    {
      var values = categories
          .Select(cat => periodData.GetValueOrDefault((period, cat)))
          .ToList();

      if (values.All(v => v == 0))
        continue;

      var formatted = values.Select(v => v.ToString("+0.00;-0.00;0.00"));

      writer.WriteLine($"{period.ToString(dateFormat)},{string.Join(",", formatted)}");
    }
  }
  finally
  {
    if (outputFile is not null)
      await writer.DisposeAsync();
  }
});

var rootCommand = new RootCommand("P2P Report - category report for P2P platforms");
rootCommand.Subcommands.Add(csvCommand);

return rootCommand.Parse(args).Invoke();

static DateOnly GetPeriod(DateOnly date, string groupBy) => groupBy switch
{
  "day" => date,
  "year" => new DateOnly(date.Year, 1, 1),
  _ => new DateOnly(date.Year, date.Month, 1)
};

static string[] GetCsvFiles(string path)
{
  if (File.Exists(path))
    return [path];

  if (Directory.Exists(path))
    return Directory.GetFiles(path, "*.csv");

  throw new FileNotFoundException($"Path not found: {path}");
}
