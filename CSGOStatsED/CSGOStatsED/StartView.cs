using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSGOStatsED.src;
using demojsonparser.src;
using DemoInfo;
namespace CSGOStatsED
{
    public partial class StartView : Form
    {
        public StartView()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            NPGSQLTest.test();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (var demoparser = new DemoParser(File.OpenRead(path))) //Force garbage collection since outputstream of the parser cannot be changed
            {
                GameStateGenerator.GenerateJSONFile(demoparser, path);
            }
        }
    }
}
