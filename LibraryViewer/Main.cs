using AltiumSharp;
using AltiumSharp.Drawing;
using OpenMcdf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AltiumSharp.BasicTypes;
using AltiumSharp.Records;
using IComponent = AltiumSharp.IComponent;

namespace LibraryViewer
{
    public partial class Main : Form
    {
        private bool _loading;
        private PropertyViewer _propertyViewer;
        private Renderer _renderer;
        private bool _autoZoom;
        private Point _mouseLocation;
        private Image _pictureBoxImage;

        private List<IComponent> _components;
        private IComponent _activeComponent;
        private IEnumerable<Primitive> _activePrimitives;
        private Unit _displayUnit;
        private Coord _snapGridSize;

        public Main()
        {
            InitializeComponent();

            pictureBox.MouseWheel += PictureBox_MouseWheel;
            typeof(Panel).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, pictureBox, new object[] { true });
        }

        private void LoadComponents(string fileName)
        {
            _loading = true;
            try
            {
                SetActiveComponent(null);
                if (_propertyViewer != null)
                {
                    _propertyViewer.Close();
                    _propertyViewer = null;
                }

                gridPcbLibComponents.Rows.Clear();
                gridSchLibComponents.Rows.Clear();
                Application.DoEvents();

                if (Path.GetExtension(fileName).Equals(".pcblib", StringComparison.InvariantCultureIgnoreCase))
                {
                    tabComponents.SelectTab(tabPcbLib);
                    using (var reader = new PcbLibReader(fileName))
                    {
                        reader.Read();

                        foreach (var component in reader.Components)
                        {
                            var index = gridPcbLibComponents.Rows.Add(component.Name, component.Pads, component.Primitives.Count());
                            gridPcbLibComponents.Rows[index].Tag = component;
                        }

                        _displayUnit = reader.Header.DisplayUnit;
                        _snapGridSize = reader.Header.SnapGridSize;
                        _components = reader.Components.Cast<IComponent>().ToList();
                        _renderer = new PcbLibRenderer();
                    }
                }
                else if (Path.GetExtension(fileName).Equals(".schlib", StringComparison.InvariantCultureIgnoreCase))
                {
                    tabComponents.SelectTab(tabSchLib);
                    using (var reader = new SchLibReader(fileName))
                    {
                        reader.Read();

                        foreach (var component in reader.Components)
                        {
                            var index = gridSchLibComponents.Rows.Add(component.Name, component.Description);
                            gridSchLibComponents.Rows[index].Tag = component;
                        }

                        _displayUnit = reader.Header.DisplayUnit;
                        _snapGridSize = reader.Header.SnapGridSize;
                        _components = reader.Components.Cast<IComponent>().ToList();
                        _renderer = new SchLibRenderer(reader.Header, reader.EmbeddedImages);
                    }
                }

                SetActiveComponent(_components.FirstOrDefault());
            }
            finally
            {
                _loading = false;
            }
        }

        private void LoadPrimitives(IComponent component, bool activateFirst = false)
        {
            Primitive defaultPrimitive = null;
            if (component is PcbComponent pcbComponent)
            {
                var primitives = pcbComponent.Primitives;
                foreach (var primitive in primitives)
                {
                    var info = primitive.GetDisplayInfo();
                    var i = gridPcbLibPrimitives.Rows.Add(primitive.Type, info.Name, info.SizeX?.ToString(_displayUnit), info.SizeY?.ToString(_displayUnit), primitive.Layer.Name);
                    gridPcbLibPrimitives.Rows[i].Tag = primitive;
                }
                defaultPrimitive = pcbComponent.Primitives.FirstOrDefault();
            }
            else if (component is SchComponent schComponent)
            {
                var pins = schComponent.Primitives.OfType<PinRecord>();
                foreach (var pin in pins)
                {
                    var i = gridSchLibPrimitives.Rows.Add(pin.Designator, pin.Name, pin.Electrical.ToString());
                    gridSchLibPrimitives.Rows[i].Tag = pin;
                }
                defaultPrimitive = pins.FirstOrDefault();
            }
            SetActivePrimitives((activateFirst && defaultPrimitive != null) ? new[] { defaultPrimitive } : null);
        }

        private void Draw(bool fastRendering)
        {
            if (_activeComponent == null)
            {
                _renderer = null;
            }

            if (_renderer != null)
            {
                _renderer.Component = _activeComponent;
                if (_pictureBoxImage == null) _pictureBoxImage = new Bitmap(pictureBox.Width, pictureBox.Height);
                using (var g = Graphics.FromImage(_pictureBoxImage))
                {
                    _renderer.Render(g, pictureBox.Width, pictureBox.Height, _autoZoom, fastRendering);
                }
                _autoZoom = false;
            }

            if (fastRendering)
            {
                redrawTimer.Stop();
                redrawTimer.Start();
            }

            pictureBox.Invalidate();
        }

        public void ExportStream(string inputFileName, string streamPath, string outputFileName)
        {
            using (var stream = new FileStream(inputFileName, FileMode.Open))
            using (var cf = new CompoundFile(stream))
            using (var ms = cf.GetStream(streamPath).GetMemoryStream())
            using (var fs = new FileStream(outputFileName, FileMode.Create, FileAccess.Write))
            {
                ms.CopyTo(fs);
            }
        }

        private void ShowPropertyViewer()
        {
            if (_propertyViewer == null)
            {
                _propertyViewer = new PropertyViewer();
                _propertyViewer.Owner = this;
                _propertyViewer.FormClosed += (s, a) => _propertyViewer = null;
                _propertyViewer.Show(this);
            }
            _propertyViewer.SetSelectedObjects(_activePrimitives.ToArray());
            _propertyViewer.BringToFront();
            _propertyViewer.Focus();
        }

        private void SetActiveComponent(IComponent component)
        {
            _activeComponent = component;
            _autoZoom = true;

            panelPart.Visible = false;
            gridPcbLibPrimitives.Rows.Clear();
            gridSchLibPrimitives.Rows.Clear();

            if (_activeComponent != null)
            {
                LoadPrimitives(_activeComponent);

                if (_activeComponent is SchComponent schComponent)
                {
                    panelPart.Visible = schComponent.PartCount > 1;
                    editPart.Maximum = schComponent.PartCount;
                    editPart.Value = 1;
                    labelPartTotal.Text = $"of {editPart.Maximum}";
                }
            }
            else
            {
                SetActivePrimitives(null);
            }
            Draw(false);
        }

        private void SetActivePrimitives(IEnumerable<Primitive> primitives)
        {
            _activePrimitives = primitives;

            _propertyViewer?.SetSelectedObjects(_activePrimitives?.ToArray());

            if (_renderer != null)
            {
                _renderer.SelectedPrimitives = primitives;
                Draw(false);
            }
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadComponents(openFileDialog.FileName);
            }
        }
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void GridPcbLibComponents_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (_loading || gridPcbLibComponents.SelectedRows.Count == 0) return;

            var component = (PcbComponent)gridPcbLibComponents.Rows[gridPcbLibComponents.SelectedRows[0].Index].Tag;
            SetActiveComponent(component);
        }

        private void GridSchLibComponents_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (_loading || gridSchLibComponents.SelectedRows.Count == 0) return;

            var component = (SchComponent)gridSchLibComponents.Rows[gridSchLibComponents.SelectedRows[0].Index].Tag;
            SetActiveComponent(component);
        }

        private void GridPcbLibPrimitives_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (_loading || e.RowIndex < 0 || e.ColumnIndex != gridPcbLibPrimitivesLayer.Index) return;

            // Draw standard cell background
            e.PaintBackground(e.CellBounds, (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected);

            // Draw layer color
            var primitive = (PcbPrimitive)gridPcbLibPrimitives.Rows[e.RowIndex].Tag;
            var layerColor = primitive.Layer.Color;
            using (var layerColorBrush = new SolidBrush(layerColor))
            {
                e.Graphics.FillRectangle(layerColorBrush, e.CellBounds.Left, e.CellBounds.Top + 1, 3, e.CellBounds.Height - 2);
                e.PaintContent(e.CellBounds);
                e.Handled = true;
            }
        }

        private void GridPcbLibPrimitives_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (_loading || gridPcbLibPrimitives.SelectedRows.Count == 0) return;

            var primitives = gridPcbLibPrimitives.SelectedRows.OfType<DataGridViewRow>()
                .Select(row => (PcbPrimitive)row.Tag).ToArray();
            SetActivePrimitives(primitives);
        }

        private void EditPart_ValueChanged(object sender, EventArgs e)
        {
            if (_renderer is SchLibRenderer schLibRenderer)
            {
                schLibRenderer.Part = (int)editPart.Value;
                _autoZoom = true;
                Draw(false);
            }
        }

        private void GridSchLibPrimitives_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (_loading || gridSchLibPrimitives.SelectedRows.Count == 0) return;

            var primitives = gridSchLibPrimitives.SelectedRows.OfType<DataGridViewRow>()
                .Select(row => (SchPrimitive)row.Tag).ToArray();
            SetActivePrimitives(primitives);
        }

        private void GridPrimitives_DoubleClick(object sender, EventArgs e)
        {
            if (_activePrimitives?.Any() == false) return;

            ShowPropertyViewer();
        }

        private void CenterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _autoZoom = true;
            Draw(false);
        }

        private void ZoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_renderer == null) return;

            var zoom = Convert.ToDouble(((ToolStripMenuItem)sender).Tag) / 100.0;
            _renderer.Zoom = zoom;
            
            _autoZoom = false;
            Draw(false);
        }

        private void ExportFootprintToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (_activeComponent == null) return;

            var component = _activeComponent;
            var streamPath = $"{component.Name.Replace('/', '_')}/Data";
            saveFileDialog.FileName = streamPath.Replace('/', '_') + ".bin";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                ExportStream(openFileDialog.FileName, streamPath, saveFileDialog.FileName);
            }
        }

        private void ExportPrimitiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_activeComponent == null || (_activePrimitives?.Count() != 1)) return;

            var component = _activeComponent;
            var primitive = _activePrimitives.First();
            if (component is PcbComponent pcbComponent)
            {
                var pcbPrimitive = primitive as PcbPrimitive;
                var primitiveIndex = pcbComponent.Primitives.IndexOf(pcbPrimitive);
                saveFileDialog.FileName = $"pcb_{component.Name}_{primitiveIndex}_{pcbPrimitive.Type}_{pcbPrimitive.GetDisplayInfo().Name}.bin".Replace('/', '_');
            }
            else if (component is SchComponent schComponent)
            {
                var schPrimitive = primitive as SchPrimitive;
                var primitiveIndex = schComponent.Primitives.IndexOf(schPrimitive);
                saveFileDialog.FileName = $"sch_{component.Name}_{primitiveIndex}_{schPrimitive.Record}.bin".Replace('/', '_');
            }
            else
            {
                return;
            }

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                System.IO.File.WriteAllBytes(saveFileDialog.FileName, primitive.RawData.ToArray());
            }
        }

        private void ExportImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_pictureBoxImage == null) return;

            var component = _activeComponent;
            var streamPath = $"{component.Name.Replace('/', '_')}/Data";
            saveFileDialog.FileName = streamPath.Replace('/', '_') + ".jpg";

            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                _pictureBoxImage.Save(saveFileDialog.FileName);
            }
        }

        private void PictureBox_Resize(object sender, EventArgs e)
        {
            _pictureBoxImage?.Dispose();
            _pictureBoxImage = null;

            if (_activeComponent != null)
            {
                Draw(false);
            }
        }

        private void PictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Clicks == 1)
            {
                SetActivePrimitives(_renderer.Pick(e.X, e.Y));
            }
        }

        private void PictureBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_activePrimitives.Count() > 0)
            {
                ShowPropertyViewer();
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_activeComponent == null) return;

            if (e.Button == MouseButtons.Right && !_mouseLocation.IsEmpty)
            {
                pictureBox.Cursor = Cursors.Hand;
                var dX = e.X - _mouseLocation.X;
                var dY = e.Y - _mouseLocation.Y;
                if (dX != 0 || dY != 0)
                {
                    _renderer.Pan(dX, dY);
                    Draw(true);
                }
            }
            else
            {
                pictureBox.Cursor = Cursors.Default;
            }

            var location = _renderer.WorldFromScreen(new PointF(e.Location.X, e.Location.Y));
            statusLocation.Text = $"{location.ToString(_displayUnit, _snapGridSize)}    Grid: {_snapGridSize.ToString(_displayUnit)}";

            _mouseLocation = e.Location;
            pictureBox.Focus();
        }

        private void PictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_activeComponent != null)
            {
                var scaleFactor = ModifierKeys.HasFlag(Keys.Control) ? 1e-4 : 1e-3;
                _renderer.Scale *= 1.0f + (e.Delta * scaleFactor);
                Draw(true);
            }
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (_pictureBoxImage != null)
            {
                e.Graphics.DrawImageUnscaled(_pictureBoxImage, 0, 0);
            }
            else
            {
                e.Graphics.Clear(SystemColors.Control);
            }
        }

        private void RedrawTimer_Tick(object sender, EventArgs e)
        {
            if (_activeComponent != null)
            {
                Draw(false);
            }
            redrawTimer.Enabled = false;
        }
    }
}
