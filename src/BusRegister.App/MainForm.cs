using System.Globalization;

namespace BusRoutes;

public sealed class MainForm : Form
{
    private readonly TextBox count = new() { Text = "5", Width = 90, AccessibleName = "Количество маршрутов" };
    private readonly DataGridView input = CreateGrid(false);
    private readonly DataGridView output = CreateGrid(true);
    private readonly Button sort = new() { Text = "Сортировать", AutoSize = true, Enabled = false };
    private readonly Button save = new() { Text = "Сохранить CSV…", AutoSize = true, Enabled = false };
    private readonly Label status = new() { Text = "Задайте количество маршрутов и нажмите «Создать набор».", Dock = DockStyle.Fill, AutoSize = true };
    private BusRegister? register;

    public MainForm()
    {
        Text = "Реестр автобусных маршрутов";
        ClientSize = new Size(1120, 760);
        MinimumSize = new Size(920, 660);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 7 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.Controls.Add(new Label { Text = "Реестр автобусных маршрутов", Font = new Font(Font.FontFamily, 17, FontStyle.Bold), AutoSize = true }, 0, 0);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var create = new Button { Text = "Создать набор", AutoSize = true };
        toolbar.Controls.AddRange([new Label { Text = "Количество:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, count, create, sort, save]);
        layout.Controls.Add(toolbar, 0, 1);
        layout.Controls.Add(new Label { Text = "Исходные данные — заполните все ячейки", AutoSize = true }, 0, 2);
        layout.Controls.Add(input, 0, 3);
        layout.Controls.Add(new Label { Text = "Результат — число остановок ↓, затем номер маршрута ↓", AutoSize = true }, 0, 4);
        layout.Controls.Add(output, 0, 5);
        layout.Controls.Add(status, 0, 6);
        Controls.Add(layout);
        create.Click += (_, _) => CreateSet();
        sort.Click += (_, _) => SortSet();
        save.Click += (_, _) => SaveSet();
        input.CellValueChanged += (_, _) => InvalidateResult();
        input.CellBeginEdit += (_, _) => InvalidateResult();
        input.DataError += (_, e) => { e.ThrowException = false; Error("Проверьте значение ячейки."); };
    }

    private static DataGridView CreateGrid(bool readOnly)
    {
        var grid = new DataGridView {
            Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            ReadOnly = readOnly, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersWidth = 55, BackgroundColor = Color.White, MultiSelect = false,
            AccessibleName = readOnly ? "Отсортированный набор" : "Исходный набор"
        };
        string[] headers = ["Номер маршрута", "Конечная остановка №1", "Конечная остановка №2", "Число остановок"];
        foreach (string header in headers)
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, SortMode = DataGridViewColumnSortMode.NotSortable });
        grid.Columns[0].FillWeight = 65;
        grid.Columns[3].FillWeight = 65;
        return grid;
    }

    private void CreateSet()
    {
        if (!int.TryParse(count.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int n) || n <= 0)
        { Error("Количество должно быть целым числом от 1 до 2147483647."); return; }
        if (input.RowCount > 0 && MessageBox.Show(this, "Заменить текущий набор? Введённые данные будут очищены.", "Новый набор", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            // Сначала выделяем массив; при нехватке памяти старый набор остаётся доступным.
            var rows = new DataGridViewRow[n];
            for (int i = 0; i < n; i++) { rows[i] = new DataGridViewRow(); rows[i].CreateCells(input); rows[i].HeaderCell.Value = (i + 1).ToString(); }
            input.Rows.Clear();
            input.Rows.AddRange(rows);
            InvalidateResult();
            sort.Enabled = true;
            status.Text = $"Создано строк: {n}. Заполните таблицу и нажмите «Сортировать».";
        }
        catch (OutOfMemoryException) { Error("Недостаточно памяти для такого количества маршрутов. Уменьшите размер набора."); }
    }

    private void InvalidateResult()
    {
        register = null;
        output.Rows.Clear();
        save.Enabled = false;
        status.Text = "Данные изменены. Выполните сортировку для получения актуального результата.";
    }

    private void SortSet()
    {
        input.EndEdit();
        try
        {
            var buses = new Bus[input.RowCount];
            for (int i = 0; i < buses.Length; i++)
            {
                string Value(int col) => Convert.ToString(input.Rows[i].Cells[col].Value)?.Trim() ?? "";
                if (!int.TryParse(Value(0), NumberStyles.None, CultureInfo.InvariantCulture, out int route) || route <= 0)
                    throw new ArgumentException($"Строка {i + 1}: номер маршрута должен быть целым числом от 1 до 2147483647.");
                if (!int.TryParse(Value(3), NumberStyles.None, CultureInfo.InvariantCulture, out int stops) || stops <= 0)
                    throw new ArgumentException($"Строка {i + 1}: число остановок должно быть целым числом от 1 до 2147483647.");
                try { buses[i] = new Bus(route, Value(1), Value(2), stops); }
                catch (ArgumentException ex) { throw new ArgumentException($"Строка {i + 1}: {ex.Message}"); }
            }
            register = new BusRegister(buses);
            register.SortDescending();
            output.Rows.Clear();
            foreach (Bus bus in register.GetSnapshot()) output.Rows.Add(bus.RouteNumber, bus.Terminal1, bus.Terminal2, bus.StopCount);
            save.Enabled = true;
            status.Text = $"Отсортировано маршрутов: {buses.Length}. Сохраните результат в CSV.";
        }
        catch (ArgumentException ex) { Error(ex.Message); }
        catch (OutOfMemoryException) { Error("Недостаточно памяти. Уменьшите размер набора."); }
    }

    private void SaveSet()
    {
        if (register is null) return;
        using var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "bus_routes.csv", DefaultExt = "csv", AddExtension = true, OverwritePrompt = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { register.SaveToFile(dialog.FileName); status.Text = $"Результат сохранён: {dialog.FileName}"; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Error($"Не удалось сохранить файл: {ex.Message}"); }
    }

    private void Error(string message) => MessageBox.Show(this, message, "Проверка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
