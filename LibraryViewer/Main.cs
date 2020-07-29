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
using IContainer = AltiumSharp.IContainer;
using IComponent = AltiumSharp.IComponent;

namespace LibraryViewer
{
    public partial class Main : Form
    {
        private bool _loading;
        private PropertyViewer _propertyViewer;
        private object _fileData;
        private Renderer _renderer;
        private bool _autoZoom;
        private Point _mouseLocation;
        private BufferedGraphics _graphicsBuffer;
        private bool _fastRendering;

        private List<IContainer> _containers;
        private IContainer _activeContainer;
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

        private void SetData(object fileData)
        {
            var saveLoading = _loading;
            _loading = true;
            try
            {
                SetActiveContainer(null);
                if (_propertyViewer != null)
                {
                    _propertyViewer.Close();
                    _propertyViewer = null;
                }

                gridPcbLibComponents.Rows.Clear();
                gridSchLibComponents.Rows.Clear();
                treeViewStructure.Nodes.Clear();
                Application.DoEvents();

                if (fileData is PcbLib pcbLib)
                {
                    tabComponents.SelectTab(tabPcbLib);
                    foreach (var component in pcbLib.Items)
                    {
                        var index = gridPcbLibComponents.Rows.Add(component.Name, component.Pads, component.Primitives.Count());
                        gridPcbLibComponents.Rows[index].Tag = component;
                    }

                    _displayUnit = pcbLib.Header.DisplayUnit;
                    _snapGridSize = pcbLib.Header.SnapGridSize;
                    _containers = pcbLib.Items.Cast<IContainer>().ToList();
                    _renderer = new PcbLibRenderer();
                }
                else if (fileData is SchLib schLib)
                {
                    tabComponents.SelectTab(tabSchLib);

                    foreach (var component in schLib.Items)
                    {
                        var index = gridSchLibComponents.Rows.Add(component.LibReference, component.ComponentDescription);
                        gridSchLibComponents.Rows[index].Tag = component;
                    }

                    _displayUnit = schLib.Header.DisplayUnit;
                    _snapGridSize = schLib.Header.SnapGridSize;
                    _containers = schLib.Items.Cast<IContainer>().ToList();

                    using (var reader = new SchLibReader())
                    {
                        var assetsData = reader.Read("assets.schlib");
                        _renderer = new SchLibRenderer(schLib.Header, assetsData);
                    }
                }
                else if (fileData is SchDoc schDoc)
                {
                    tabComponents.SelectTab(tabSchLib);

                    var index = gridSchLibComponents.Rows.Add("SchDoc");
                    gridSchLibComponents.Rows[index].Tag = schDoc.Header;
                    foreach (var component in schDoc.Header.GetPrimitivesOfType<SchComponent>())
                    {
                        index = gridSchLibComponents.Rows.Add(component.LibReference, component.ComponentDescription);
                        gridSchLibComponents.Rows[index].Tag = component;
                    }

                    var sheet = schDoc.Header;
                    _displayUnit = sheet.DisplayUnit;
                    _snapGridSize = sheet.SnapGridSize;
                    _containers = new List<IContainer> { schDoc.Header };

                    using (var assetsReader = new SchLibReader())
                    {
                        var assetsData = assetsReader.Read("assets.schlib");
                        _renderer = new SchLibRenderer(schDoc.Header, assetsData);
                    }
                }

                _fileData = fileData;
                SetActiveContainer(_containers.FirstOrDefault());
            }
            finally
            {
                _loading = saveLoading;
            }
        }

        private void LoadFromFile(string fileName)
        {
            if (Path.GetExtension(fileName).Equals(".pcblib", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var reader = new PcbLibReader())
                {
                    var data = reader.Read(fileName);
                    SetData(data);
                }
            }
            else if (Path.GetExtension(fileName).Equals(".schlib", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var reader = new SchLibReader())
                {
                    var data = reader.Read(fileName);
                    SetData(data);
                }
            }
            else if (Path.GetExtension(fileName).Equals(".schdoc", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var reader = new SchDocReader())
                {
                    var data = reader.Read(fileName);
                    SetData(data);
                }
            }
        }

        private void SaveToFile(string fileName)
        {
            _loading = true;
            try
            {
                if (_fileData == null) return;

                if (_fileData is PcbLib pcbLib)
                {
                    using (var writer = new PcbLibWriter())
                    {
                        writer.Write(pcbLib, fileName, true);
                    }
                }
                else if (_fileData is SchLib schLib)
                {
                    using (var writer = new SchLibWriter())
                    {
                        writer.Write(schLib, fileName, true);
                    }
                }
                else if (_fileData is SchDoc schDoc)
                {
                    using (var writer = new SchDocWriter())
                    {
                        writer.Write(schDoc, fileName, true);
                    }
                }
            }
            finally
            {
                _loading = false;
            }
        }

        private void LoadPrimitives(IContainer component, bool activateFirst = false)
        {
            Primitive defaultPrimitive = null;
            if (component is PcbComponent pcbComponent)
            {
                var primitives = pcbComponent.Primitives;
                foreach (var primitive in primitives)
                {
                    var info = primitive.GetDisplayInfo();
                    var i = gridPcbLibPrimitives.Rows.Add(primitive.ObjectId, info.Name, info.SizeX?.ToString(_displayUnit), info.SizeY?.ToString(_displayUnit), primitive.Layer.Name);
                    gridPcbLibPrimitives.Rows[i].Tag = primitive;
                }
                defaultPrimitive = pcbComponent.Primitives.FirstOrDefault();
            }
            else if (component is SchComponent schComponent)
            {
                var pins = schComponent.GetPrimitivesOfType<SchPin>();
                foreach (var pin in pins)
                {
                    var i = gridSchLibPrimitives.Rows.Add(pin.Designator, pin.Name, pin.Electrical.ToString());
                    gridSchLibPrimitives.Rows[i].Tag = pin;
                }
                defaultPrimitive = pins.FirstOrDefault();
            }
            SetActivePrimitives((activateFirst && defaultPrimitive != null) ? new[] { defaultPrimitive } : null);
        }

        private void LoadTreeViewStructure(IContainer container)
        {
            void processContainer(TreeNodeCollection tnc, IContainer c)
            {
                foreach (var primitive in c.GetPrimitivesOfType<Primitive>(false))
                {
                    var node = tnc.Add(primitive.ToString());
                    node.Tag = primitive;

                    if (primitive is IContainer c2)
                    {
                        processContainer(node.Nodes, c2);
                    }
                }
            }

            treeViewStructure.BeginUpdate();
            processContainer(treeViewStructure.Nodes, container);
            treeViewStructure.EndUpdate();
            treeViewStructure.Refresh();
        }

        private void RequestRedraw(bool fastRendering)
        {
            _fastRendering = fastRendering;
            pictureBox.Invalidate();
            if (_fastRendering) pictureBox.Update();
        }

        private void Draw(Graphics target)
        {
            if (_activeContainer == null)
            {
                _renderer = null;
            }

            if (_renderer == null)
            {
                target.Clear(SystemColors.Control);
                return;
            }

            _renderer.Component = _activeContainer;
            if (_graphicsBuffer == null) _graphicsBuffer = BufferedGraphicsManager.Current.Allocate(CreateGraphics(), new Rectangle(0, 0, pictureBox.Width, pictureBox.Height));
            _renderer.Render(_graphicsBuffer.Graphics, pictureBox.Width, pictureBox.Height, _autoZoom, _fastRendering);
            _autoZoom = false;
            _graphicsBuffer.Render(target);

            if (_fastRendering)
            {
                redrawTimer.Stop();
                redrawTimer.Start();
            }
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
            _propertyViewer.SetSelectedObjects(_activePrimitives?.ToArray());
            _propertyViewer.BringToFront();
            _propertyViewer.Focus();
        }

        private void SetActiveContainer(IContainer component)
        {
            _activeContainer = component;
            _autoZoom = true;

            panelPart.Visible = false;
            gridPcbLibPrimitives.Rows.Clear();
            gridSchLibPrimitives.Rows.Clear();
            treeViewStructure.Nodes.Clear();

            if (_activeContainer != null)
            {
                LoadPrimitives(_activeContainer);

                if (_activeContainer is SchComponent schComponent)
                {
                    panelPart.Visible = schComponent.PartCount > 1;
                    if (panelPart.Visible)
                    {
                        editPart.Maximum = schComponent.PartCount;
                        editPart.Value = 1;
                        labelPartTotal.Text = $"of {editPart.Maximum}";
                    }
                }

                LoadTreeViewStructure(_activeContainer);
            }
            else
            {
                SetActivePrimitives(null);
            }
            RequestRedraw(false);
        }

        private void SetActivePrimitives(IEnumerable<Primitive> primitives)
        {
            _activePrimitives = primitives;

            _propertyViewer?.SetSelectedObjects(_activePrimitives?.ToArray());

            if (_renderer != null)
            {
                _renderer.SelectedPrimitives = primitives;
                RequestRedraw(false);
            }
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadFromFile(openFileDialog.FileName);
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveToFile(saveFileDialog.FileName);
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void GridPcbLibComponents_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (_loading || gridPcbLibComponents.SelectedRows.Count == 0) return;

            var container = (IContainer)gridPcbLibComponents.Rows[gridPcbLibComponents.SelectedRows[0].Index].Tag;
            SetActiveContainer(container);
        }

        private void GridSchLibComponents_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (_loading || gridSchLibComponents.SelectedRows.Count == 0) return;

            var container = (IContainer)gridSchLibComponents.Rows[gridSchLibComponents.SelectedRows[0].Index].Tag;
            SetActiveContainer(container);
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
                RequestRedraw(false);
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
            RequestRedraw(false);
        }

        private void ZoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_renderer == null) return;

            var zoom = Convert.ToDouble(((ToolStripMenuItem)sender).Tag) / 100.0;
            _renderer.Zoom = zoom;

            _autoZoom = false;
            RequestRedraw(false);
        }

        private void ExportFootprintToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (_activeContainer == null) return;

            var container = _activeContainer;
            string streamPath;
            if (container is IComponent component)
            {
                streamPath = $"{component.Name.Replace('/', '_')}/Data";
            }
            else
            {
                streamPath = "FileHeader";
            }
            saveFileDialog.FileName = streamPath.Replace('/', '_') + ".bin";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                ExportStream(openFileDialog.FileName, streamPath, saveFileDialog.FileName);
            }
        }

        private void ExportPrimitiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_activeContainer == null || (_activePrimitives?.Count() != 1)) return;

            var container = _activeContainer;
            var primitive = _activePrimitives.First();
            if (container is PcbComponent pcbComponent)
            {
                var pcbPrimitive = primitive as PcbPrimitive;
                var primitiveIndex = pcbComponent.Primitives.IndexOf(pcbPrimitive);
                saveFileDialog.FileName = $"pcb_{pcbComponent.Name}_{primitiveIndex}_{pcbPrimitive.GetDisplayInfo().Name}.bin".Replace('/', '_');
            }
            else if (container is SchComponent schComponent)
            {
                var schPrimitive = primitive as SchPrimitive;
                var primitiveId = schPrimitive.GetHashCode();
                saveFileDialog.FileName = $"sch_{schComponent.LibReference}_{primitiveId}_{schPrimitive.Record}.bin".Replace('/', '_');
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
            if (_activeContainer == null || _graphicsBuffer == null) return;

            saveFileDialog.FileName = Path.ChangeExtension(openFileDialog.FileName, ".png");

            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                using (var bitmap = new Bitmap(pictureBox.ClientSize.Width, pictureBox.ClientSize.Height))
                using (var target = Graphics.FromImage(bitmap))
                {
                    Draw(target);
                    bitmap.Save(saveFileDialog.FileName);
                }
            }
        }

        private void PictureBox_Resize(object sender, EventArgs e)
        {
            _graphicsBuffer?.Dispose();
            _graphicsBuffer = null;

            if (_activeContainer != null)
            {
                RequestRedraw(false);
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
            if (_activeContainer == null) return;

            if (e.Button == MouseButtons.Right && !_mouseLocation.IsEmpty)
            {
                pictureBox.Cursor = Cursors.Hand;
                var dX = e.X - _mouseLocation.X;
                var dY = e.Y - _mouseLocation.Y;
                if (dX != 0 || dY != 0)
                {
                    _renderer.Pan(dX, dY);
                    RequestRedraw(true);
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
            if (_activeContainer != null)
            {
                var scaleFactor = ModifierKeys.HasFlag(Keys.Control) ? 1e-4 : 1e-3;
                _renderer.Scale *= 1.0f + (e.Delta * scaleFactor);
                RequestRedraw(true);
            }
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            Draw(e.Graphics);
        }

        private void RedrawTimer_Tick(object sender, EventArgs e)
        {
            if (_activeContainer != null)
            {
                RequestRedraw(false);
            }
            redrawTimer.Enabled = false;
        }

        private void treeViewStructure_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var primitive = (Primitive)e.Node.Tag;
            IEnumerable<Primitive> primitives = new[] { primitive };
            if (primitive is IContainer container)
            {
                primitives = Enumerable.Concat(primitives, container.GetPrimitivesOfType<Primitive>());
            }
            SetActivePrimitives(primitives);
        }

        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetActiveContainer(null);

            var schLib = new SchLib
            {
                new SchComponent
                {
                    new SchRectangle
                    {
                        Corner = CoordPoint.FromMils(500, 1100)
                    },
                    new SchPin
                    {
                        Location = CoordPoint.FromMils(500, 100)
                    },
                    new SchPin
                    {
                        Location = CoordPoint.FromMils(500, 250)
                    },
                    new SchPin
                    {
                        Location = CoordPoint.FromMils(500, 400)
                    },
                    new SchPin
                    {
                        Location = CoordPoint.FromMils(500, 550)
                    },
                    new SchPin
                    {
                        Location = CoordPoint.FromMils(500, 700)
                    },
                    new SchPin
                    {
                        Location = CoordPoint.FromMils(500, 850)
                    },
                    new SchPin
                    {
                        Location = CoordPoint.FromMils(500, 1000)
                    },
                    new SchPin
                    {
                        Designator = "P8",
                        Location = CoordPoint.FromMils(0, 1000),
                        Orientation = TextOrientations.Flipped
                    },
                    new SchPin
                    {
                        Location = CoordPoint.FromMils(0, 850),
                        Orientation = TextOrientations.Flipped
                    },
                    new SchPin
                    {
                        Location = CoordPoint.FromMils(0, 700),
                        Orientation = TextOrientations.Flipped
                    },
                    new SchPin
                    {
                        Location = CoordPoint.FromMils(0, 550),
                        Orientation = TextOrientations.Flipped
                    },
                    new SchPin
                    {
                        Location = CoordPoint.FromMils(0, 400),
                        Orientation = TextOrientations.Flipped
                    },
                    new SchPin
                    {
                        Location = CoordPoint.FromMils(0, 250),
                        Orientation = TextOrientations.Flipped
                    },
                    new SchPin
                    {
                        Location = CoordPoint.FromMils(0, 100),
                        Orientation = TextOrientations.Flipped
                    }
                }
            };
            SetData(schLib);
        }
    }
}
