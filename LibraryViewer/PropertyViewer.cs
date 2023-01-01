using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OriginalCircuit.AltiumSharp;

namespace LibraryViewer
{
    public partial class PropertyViewer : Form
    {
        private object[] _objects;

        internal event EventHandler Changed;

        public PropertyViewer()
        {
            InitializeComponent();
        }

        private void PropertyViewer_Load(object sender, EventArgs e)
        {
            CenterToParent();
        }

        internal void SetSelectedObjects(object[] objects)
        {
            _objects = objects;
            comboBoxObjects.BeginUpdate();
            comboBoxObjects.Items.Clear();
            if (_objects?.Length > 0)
            {
                if (_objects.Length != 1)
                {
                    comboBoxObjects.Items.Add("<Common Properties>");
                }
                comboBoxObjects.Items.AddRange(_objects);
                comboBoxObjects.SelectedIndex = 0;
            }
            else
            {
                comboBoxObjects.SelectedIndex = -1;
            }
            comboBoxObjects.EndUpdate();
        }

        private void ComboBoxObjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_objects.Length != 1 && comboBoxObjects.SelectedIndex < 1)
            {
                propertyGrid.SelectedObjects = _objects;
            }
            else
            {
                propertyGrid.SelectedObject = comboBoxObjects.SelectedItem;
            }
        }

        private void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
