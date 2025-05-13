using Google.OrTools.LinearSolver;
using OperationsResearch;
using ORToolsSampleWinForms.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Windows.Forms;
using OfficeOpenXml;

namespace ORToolsSampleWinForms
{
    public partial class Form1 : Form
    {

        private string _selectedFilePath = string.Empty;
        private bool IsExcelFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext == ".xlsx" || ext == ".xls";
        }
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0 && File.Exists(files[0]) && IsExcelFile(files[0]))
            {
                _selectedFilePath = files[0];
                labelFilePath.Text = $"Перетащен файл: {Path.GetFileName(_selectedFilePath)}";
            }
            else
            {
                MessageBox.Show("Некорректный файл!");
            }
        }

        public Form1()
        {
            InitializeComponent();
            this.AllowDrop = true; // Включить Drag&Drop для формы
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void buttonCalc_Click(object sender, EventArgs e)
        {
            var s = new Stopwatch();
            s.Start();

            for (int i = 0; i < 1; i++)
            {
                SolveProblem();
            }
            s.Stop();

            MessageBox.Show("Прошло миллисекунд: " + s.ElapsedMilliseconds);
        }
        private (List<FurnaceData>, WorkshopData) ReadExcelData(string filePath)
        {
            ExcelPackage.License.SetNonCommercialPersonal("Andrew Besprozvanny");
            var furnaces = new List<FurnaceData>();
            var workshop = new WorkshopData();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var sheet = package.Workbook.Worksheets["Sheet1"];

                // Находим заголовок "Доменные печи" (предположим, он в ячейке B2)
                int headerRow = 2;
                int startCol = 2; // Столбец B

                // Считаем количество заполненных столбцов справа от заголовка
                int col = startCol;
                while (sheet.Cells[headerRow, col].Value != null)
                {
                    col++;
                }
                int furnaceCount = col - startCol;

                // Чтение данных для каждой печи
                for (int i = 0; i < furnaceCount; i++)
                {
                    int currentCol = startCol + i;
                    var furnace = new FurnaceData
                    {
                        Id = i + 1,
                        BaseGasFlow = sheet.Cells[3, currentCol].GetValue<double>(),
                        MinGasFlow = sheet.Cells[4, currentCol].GetValue<double>(),
                        MaxGasFlow = sheet.Cells[5, currentCol].GetValue<double>(),
                        BaseCokeConsumption = sheet.Cells[6, currentCol].GetValue<double>(),
                        CokeEquiv = sheet.Cells[7, currentCol].GetValue<double>(),
                        BaseIronProduction = sheet.Cells[8, currentCol].GetValue<double>(),
                        BaseSilicon = sheet.Cells[9, currentCol].GetValue<double>(),
                        MinSilicon = sheet.Cells[10, currentCol].GetValue<double>(),
                        MaxSilicon = sheet.Cells[11, currentCol].GetValue<double>(),
                        DeltaIronGas = sheet.Cells[13, currentCol].GetValue<double>(),
                        DeltaIronCoke = sheet.Cells[14, currentCol].GetValue<double>(),
                        DeltaSiGas = sheet.Cells[15, currentCol].GetValue<double>(),
                        DeltaSiCoke = sheet.Cells[16, currentCol].GetValue<double>(),
                        DeltaSiProduction = sheet.Cells[17, currentCol].GetValue<double>()
                    };
                    furnaces.Add(furnace);
                }

                // Чтение данных цеха
                workshop.TotalGasReserve = sheet.Cells[20, 2].GetValue<double>();
                workshop.TotalCokeReserve = sheet.Cells[21, 2].GetValue<double>();
                workshop.RequiredIron = sheet.Cells[22, 2].GetValue<double>();
            }

            return (furnaces, workshop);
        }
        private double CalculateAi(FurnaceData furnace)
        {
            return furnace.DeltaSiGas
                   - furnace.CokeEquiv * furnace.DeltaSiCoke
                   + (furnace.DeltaIronGas - furnace.CokeEquiv * furnace.DeltaIronCoke)
                     * furnace.DeltaSiProduction;
        }
        private void CalculateSiliconBounds(FurnaceData furnace)
        {
            double Ai = CalculateAi(furnace);
            furnace.Xmin = furnace.BaseGasFlow + (furnace.MinSilicon - furnace.BaseSilicon) / Ai;
            furnace.Xmax = furnace.BaseGasFlow + (furnace.MaxSilicon - furnace.BaseSilicon) / Ai;
        }
        private void ApplyGasConstraints(FurnaceData furnace)
        {
            furnace.VpgMax = Math.Min(furnace.MaxGasFlow, furnace.Xmax);
            furnace.VpgMin = Math.Max(furnace.MinGasFlow, furnace.Xmin);
            // Заглушка, возможно сделать лучше
            if (furnace.Xmin > furnace.Xmax)
            {
                furnace.VpgMax = Math.Min(furnace.MaxGasFlow, furnace.Xmin);
                furnace.VpgMin = Math.Max(furnace.MinGasFlow, furnace.Xmax);
            }
        }
        private List<SolverVar> PrepareSolverVars(List<FurnaceData> furnaces)
        {
            var solverVars = new List<SolverVar>();

            foreach (var furnace in furnaces)
            {
                CalculateSiliconBounds(furnace);
                ApplyGasConstraints(furnace);

                var constraintKoefs = new double[]
                {
            1,
            -0.001 * furnace.CokeEquiv,
            furnace.DeltaIronGas - furnace.CokeEquiv * furnace.DeltaIronCoke
                };

                solverVars.Add(new SolverVar
                {
                    xId = furnace.Id,
                    Koef = constraintKoefs[2],
                    Min = furnace.VpgMin,
                    Max = furnace.VpgMax,
                    ConstraintKoefs = constraintKoefs
                });
            }

            return solverVars;
        }
        private double CalculateCokeConstraintRhs(List<FurnaceData> furnaces, double totalCokeReserve)
        {
            double sum = furnaces.Sum(f =>
                f.BaseCokeConsumption + 0.001 * f.CokeEquiv * f.BaseGasFlow
            );
            return totalCokeReserve - sum;
        }


        private double CalculateProductionConstraintRhs(List<FurnaceData> furnaces, double requiredIron)
        {
            double sumBaseProduction = furnaces.Sum(f => f.BaseIronProduction);
            double sumDelta = furnaces.Sum(f =>
                (f.DeltaIronGas - f.CokeEquiv * f.DeltaIronCoke) * f.BaseGasFlow
            );
            return requiredIron - sumBaseProduction + sumDelta;
        }
        private void SolveProblem()
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                MessageBox.Show("Файл не выбран!");
                return;
            }


            var (furnaces, workshop) = ReadExcelData(_selectedFilePath);

            // Рассчитываем правые части ограничений
            double cokeRhs = CalculateCokeConstraintRhs(furnaces, workshop.TotalCokeReserve);
            double productionRhs = CalculateProductionConstraintRhs(furnaces, workshop.RequiredIron);

            var rawConstraints = new List<SolverConstraint>
                {
                    new SolverConstraint { Name = "Gas", Lb = double.NegativeInfinity, Ub = workshop.TotalGasReserve },
                    new SolverConstraint { Name = "Coke", Lb = double.NegativeInfinity, Ub = cokeRhs },
                    new SolverConstraint { Name = "Production", Lb = productionRhs, Ub = double.PositiveInfinity }
                };

            var rawData = PrepareSolverVars(furnaces);

            Solver solver = Solver.CreateSolver("GLOP");
            var vars = new List<Variable>();

            // Создание переменных
            foreach (var data in rawData)
            {
                vars.Add(solver.MakeNumVar(data.Min, data.Max, data.xId.ToString()));
            }

            // Добавление ограничений
            var constraints = new List<Constraint>();
            for (int i = 0; i < rawConstraints.Count; ++i)
            {
                var constraint = solver.MakeConstraint(
                    rawConstraints[i].Lb,
                    rawConstraints[i].Ub,
                    rawConstraints[i].Name
                );

                for (int j = 0; j < vars.Count; ++j)
                {
                    constraint.SetCoefficient(vars[j], rawData[j].ConstraintKoefs[i]);
                }

                constraints.Add(constraint);
            }

            // Целевая функция (максимизация)
            var objective = solver.Objective();
            for (int i = 0; i < vars.Count; ++i)
            {
                objective.SetCoefficient(vars[i], rawData[i].Koef);
            }
            objective.SetMaximization();

            Solver.ResultStatus resultStatus = solver.Solve();

            // Check that the problem has an optimal solution.
            if (resultStatus != Solver.ResultStatus.OPTIMAL)
            {
                toolStripStatusLabel.Text = "Optimal solution not found. ";

                if (resultStatus == Solver.ResultStatus.FEASIBLE)
                {
                    toolStripStatusLabel.Text += "Suboptimal solution was found.";
                }
                else
                {
                    toolStripStatusLabel.Text += "Could not solve the problem.";
                    return;
                }
            }

            var reportStr = $"Problem solved in {solver.Iterations()} iterations for {solver.WallTime()} milliseconds.\n" +
                $"Objective function value: {objective.Value()}\n\n";

            for (int i = 0; i < rawData.Count; i++)
            {
                reportStr += $"Optimal value of var {rawData[i].xId} - {vars[i].SolutionValue()}\n";
            }

            MessageBox.Show(reportStr);
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void buttonSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Excel Files|*.xlsx;*.xls";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedFilePath = openFileDialog.FileName;
                    labelFilePath.Text = $"Выбран файл: {Path.GetFileName(_selectedFilePath)}";
                }
            }
        }
    }
}