using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AltiumSharp;

namespace LibraryViewer
{
    public partial class PropertyViewer : Form
    {
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
            propertyGrid.SelectedObjects = objects;
        }
    }
}
