namespace BusRoutes;

/// <summary>Неизменяемые данные одного автобусного маршрута.</summary>
public sealed class Bus
{
    public int RouteNumber { get; }
    public string Terminal1 { get; }
    public string Terminal2 { get; }
    public int StopCount { get; }

    public Bus(int routeNumber, string terminal1, string terminal2, int stopCount)
    {
        if (routeNumber <= 0) throw new ArgumentOutOfRangeException(nameof(routeNumber), "Номер маршрута должен быть положительным целым числом.");
        if (stopCount <= 0) throw new ArgumentOutOfRangeException(nameof(stopCount), "Число остановок должно быть положительным целым числом.");
        if (string.IsNullOrWhiteSpace(terminal1)) throw new ArgumentException("Укажите конечную остановку №1.", nameof(terminal1));
        if (string.IsNullOrWhiteSpace(terminal2)) throw new ArgumentException("Укажите конечную остановку №2.", nameof(terminal2));
        RouteNumber = routeNumber;
        Terminal1 = terminal1.Trim();
        Terminal2 = terminal2.Trim();
        StopCount = stopCount;
    }
}
