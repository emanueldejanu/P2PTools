namespace P2PReport.Adapters;

public interface IPlatformAdapter
{
  string Name { get; }

  Task<IReadOnlyList<Transaction>> LoadTransactionsAsync(string[] files);
}
