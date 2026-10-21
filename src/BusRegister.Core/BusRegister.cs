using System.Text;

namespace BusRoutes;

/// <summary>Хранит одномерный массив Bus, сортирует его и сохраняет в CSV.</summary>
public sealed class BusRegister
{
    private readonly Bus[] buses;

    public BusRegister(Bus[] source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (Array.Exists(source, bus => bus is null))
            throw new ArgumentException("Массив содержит незаполненный маршрут.", nameof(source));
        buses = (Bus[])source.Clone();
    }

    public Bus[] GetSnapshot() => (Bus[])buses.Clone();

    /// <summary>Устойчивая сортировка вставками по StopCount DESC, RouteNumber DESC.
    /// O(n²) по времени в худшем случае, O(1) дополнительной памяти.</summary>
    public void SortDescending()
    {
        for (int i = 1; i < buses.Length; i++)
        {
            Bus current = buses[i];
            int j = i - 1;
            while (j >= 0 && (buses[j].StopCount < current.StopCount ||
                   (buses[j].StopCount == current.StopCount && buses[j].RouteNumber < current.RouteNumber)))
            {
                buses[j + 1] = buses[j];
                j--;
            }
            buses[j + 1] = current;
        }
    }

    /// <summary>CSV с разделителем ;, UTF-8 BOM, с экранированием кавычек и переводов строк.</summary>
    public void SaveToFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
        writer.WriteLine("Номер маршрута;Конечная остановка №1;Конечная остановка №2;Число остановок");
        foreach (Bus bus in buses)
            writer.WriteLine($"{bus.RouteNumber};{Escape(bus.Terminal1)};{Escape(bus.Terminal2)};{bus.StopCount}");
    }

    private static string Escape(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
