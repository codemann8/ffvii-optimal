﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using FFVIIHighwind.Objects;

namespace FFVIIHighwind
{
    public partial class MainForm : Form
    {
        private Controller c;

        ToolTip nodeTip = new ToolTip();

        public MainForm()
        {
            InitializeComponent();

            spltLeftRight.SplitterDistance = 78;
            spltTopBottom.SplitterDistance = 35;

            c = new Controller();
        }

        private void grpCharacters_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radio = (RadioButton)sender;

            if (radio.Checked)
            {
                switch (radio.Name)
                {
                    case "rdoCloud":
                        c.InitCharacter(new CloudCharacter());
                        break;
                    case "rdoBarret":
                        c.InitCharacter(new BarretCharacter());
                        break;
                    case "rdoTifa":
                        c.InitCharacter(new TifaCharacter());
                        break;
                    case "rdoAeris":
                        c.InitCharacter(new AerisCharacter());
                        break;
                    case "rdoRed":
                        c.InitCharacter(new RedCharacter());
                        break;
                    case "rdoYuffie":
                        c.InitCharacter(new YuffieCharacter());
                        break;
                    case "rdoCait":
                        c.InitCharacter(new CaitCharacter());
                        break;
                    case "rdoVincent":
                        c.InitCharacter(new VincentCharacter());
                        break;
                    case "rdoCid":
                        c.InitCharacter(new CidCharacter());
                        break;
                }

                cboLevel.Items.Clear();
                for (byte l = c.ActiveTree.Character.StartLevel; l <= Controller.MAX_LEVEL; l++)
                {
                    cboLevel.Items.Add(l);
                }

                treePath.Enabled = false;
                Application.DoEvents();

                LoadTree();

                treePath.Enabled = true;
                btnClear.Enabled = true;
                btnSet.Enabled = true;
                btnSimulate.Enabled = true;
            }
        }

        private void LoadTree()
        {
            if (c.ActiveTree.LevelIndex[Controller.MAX_LEVEL - 1].Count == 0)
            {
                stsStatus.BackColor = Color.Coral;

                string fileName = String.Format(@"{0}\FFVIICache\{1}.hpmp", AppDomain.CurrentDomain.BaseDirectory, c.ActiveTree.Character.GetFilenamePrefix());

                if (File.Exists(fileName))
                {
                    lblStatus.Text = "Loading...";
                    Application.DoEvents();

                    int memory = c.GetMemory(c.ActiveTree.Character);

                    c.ActiveTree.ImportTree(fileName, memory);
                }
                else
                {
                    lblStatus.Text = "Building...";
                    Application.DoEvents();

                    c.BuildTree();

                    lblStatus.Text = "Trimming...";
                    Application.DoEvents();

                    c.ActiveTree.RemoveSubPars();

                    lblStatus.Text = "Saving...";
                    Application.DoEvents();

                    c.ActiveTree.ExportTree(fileName);
                }

                if (c.ActiveTree.RootNode.Level != c.ActiveTree.Character.StartLevel)
                {
                    lblStatus.Text = "Analyzing...";
                    Application.DoEvents();

                    c.ActiveTree.FindMinMaxPath();
                    c.ActiveTree.FindProbableResets();
                    c.ActiveTree.FindSmartResets();
                }

                lblStatus.Text = "Idle";
                stsStatus.BackColor = SystemColors.Control;
            }

            RefreshTree();
        }

        private void RefreshTree()
        {
            treePath.Nodes.Clear();

            TreeNode node;

            if (c.ActiveTree.RootNode != null)
            {
                node = new TreeNode(c.ActiveTree.RootNode.ToString());
                node.Tag = c.ActiveTree.RootNode;
            }
            else
            {
                node = new TreeNode(c.ActiveTree.RootNode.ToString());
                node.Tag = c.ActiveTree.RootNode;
            }

            node.Nodes.Add(new TreeNode(""));

            treePath.Nodes.Add(node);

            treePath.Nodes[0].Expand();
        }

        private void treePath_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes[0].Text == "")
            {
                Node tag = null;

                if (e.Node.Tag is Node)
                {
                    tag = (Node)e.Node.Tag;
                }
                else if (e.Node.Tag is NodeLink)
                {
                    tag = ((NodeLink)e.Node.Tag).Child;
                }

                foreach (NodeLink child in tag.ChildNodes)
                {
                    if (child.Child != null)
                    {
                        TreeNode node = new TreeNode(child.ToString());
                        node.Tag = child;
                        node.Nodes.Add(new TreeNode(""));
                        switch (child.Quality)
                        {
                            case Quality.Good:
                                node.ForeColor = Color.FromName("Green");
                                break;
                            case Quality.Bad:
                                node.ForeColor = Color.FromName("Red");
                                break;
                            case Quality.Questionable:
                                node.ForeColor = Color.FromName("Goldenrod");
                                break;
                        }

                        e.Node.Nodes.Add(node);
                    }
                }

                e.Node.Nodes.RemoveAt(0);
            }
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            stsStatus.BackColor = Color.Coral;
            lblStatus.Text = "Setting...";
            Application.DoEvents();

            byte level;
            ushort hp, mp;

            bool success = false;

            if (Byte.TryParse(cboLevel.Text, out level) && level > 0 && UInt16.TryParse(txtHP.Text, out hp) && hp > 0 && UInt16.TryParse(txtMP.Text, out mp) && mp > 0)
            {
                if (c.SetMemory(c.ActiveTree.Character, level * 10000000 + hp * 1000 + mp))
                {
                    success = true;

                    c.ActiveTree.RootNode = c.ActiveTree.FindNode(level, hp, mp);

                    cboLevel.ResetText();
                    txtHP.Text = "";
                    txtMP.Text = "";
                }
                else
                {
                    MessageBox.Show("The values entered are NOT safe values");
                }
            }
            else if (treePath.SelectedNode != null && treePath.SelectedNode.Tag != null)
            {
                success = true;

                if (treePath.SelectedNode.Tag is Node)
                {
                    c.ActiveTree.RootNode = (Node)treePath.SelectedNode.Tag;
                }
                else if (treePath.SelectedNode.Tag is NodeLink)
                {
                    c.ActiveTree.RootNode = ((NodeLink)treePath.SelectedNode.Tag).Child;
                }

                c.SetMemory(c.ActiveTree.Character, c.ActiveTree.RootNode.Level * 10000000 + c.ActiveTree.RootNode.HP * 1000 + c.ActiveTree.RootNode.MP);
            }

            if (success)
            {
                lblStatus.Text = "Analyzing...";
                Application.DoEvents();

                c.ActiveTree.FindMinMaxPath();
                c.ActiveTree.FindProbableResets();
                c.ActiveTree.FindSmartResets();

                RefreshTree();
            }

            lblStatus.Text = "Idle";
            stsStatus.BackColor = SystemColors.Control;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            stsStatus.BackColor = Color.Coral;
            lblStatus.Text = "Resetting...";
            Application.DoEvents();

            c.SetMemory(c.ActiveTree.Character, 0);

            c.ActiveTree = new CharacterTree(c.ActiveTree.Character);

            LoadTree();

            lblStatus.Text = "Idle";
            stsStatus.BackColor = SystemColors.Control;
        }

        private void btnSimulate_Click(object sender, EventArgs e)
        {
            stsStatus.BackColor = Color.Coral;
            lblStatus.Text = "Simulating...";
            Application.DoEvents();

            Node selectedNode = c.ActiveTree.RootNode;

            if (treePath.SelectedNode != null && treePath.SelectedNode.Tag != null)
            {
                if (treePath.SelectedNode.Tag is Node)
                {
                    selectedNode = (Node)treePath.SelectedNode.Tag;
                }
                else if (treePath.SelectedNode.Tag is NodeLink)
                {
                    selectedNode = ((NodeLink)treePath.SelectedNode.Tag).Child;
                }
            }

            int numberOfSimulations = 10000;
            int countTotalResets = 0, minResets = 9999, maxResets = 0;

            for (int i = 0; i < numberOfSimulations; i++)
            {
                int resets = SimulateMaxSafeOnly(selectedNode);
                countTotalResets += resets;
                minResets = Math.Min(minResets, resets);
                maxResets = Math.Max(maxResets, resets);
            }

            int countTotalResets2 = 0, minResets2 = 9999, maxResets2 = 0;

            for (int i = 0; i < numberOfSimulations; i++)
            {
                int resets = SimulateMaxBetterSafe(selectedNode);
                countTotalResets2 += resets;
                minResets2 = Math.Min(minResets2, resets);
                maxResets2 = Math.Max(maxResets2, resets);
            }

            selectedNode.SimulatedResets = countTotalResets2 / (double)numberOfSimulations;

            MessageBox.Show(String.Format("{0} normal simulations, average {1:0.00} resets, range {2}-{3}\n{0} smart simulations, average {4:0.00} resets, range {5}-{6}", numberOfSimulations, countTotalResets / (double)numberOfSimulations, minResets, maxResets, countTotalResets2 / (double)numberOfSimulations, minResets2, maxResets2));

            if (treePath.SelectedNode != null)
            {
                treePath.SelectedNode.Text = selectedNode.ToString();
            }
            else
            {
                treePath.Nodes[0].Text = selectedNode.ToString();
            }

            lblStatus.Text = "Idle";
            stsStatus.BackColor = SystemColors.Control;
        }

        public int SimulateMaxBetterSafe(Node current)
        {
            int countResets = 0;
            
            System.Security.Cryptography.RNGCryptoServiceProvider provider = new System.Security.Cryptography.RNGCryptoServiceProvider();
            byte[] rs = new byte[4];
            byte randomPointer = 0;

            string output = String.Format("Level {0} ({1}/{2})", current.Level, current.HP, current.MP);
            bool shouldPrintLevel = false;

            while (current.Level < Controller.MAX_LEVEL)
            {
                bool valueIsSafe = false;

                int countResetsLevel = 0, rngIdx = 1;

                do
                {
                    if (current.Level == c.ActiveTree.Character.SafetyLevel)
                    {
                        shouldPrintLevel = true;
                    }

                    //Calculating next level's possible values
                    HPMPGradientBase table = c.ActiveTree.Character.GetTable((byte)(current.Level + 1));

                    short hpDiffBase = (short)((short)(100 * (table.HP_BASE + (current.Level) * table.HP_GRADIENT) / current.HP) - 100),
                        mpDiffBase = (short)((short)(100 * (table.MP_BASE + (short)((current.Level) * table.MP_GRADIENT / 10)) / current.MP) - 100);

                    ushort[] hps = new ushort[Controller.RNG_LIST.GetLength(0)],
                        mps = new ushort[Controller.RNG_LIST.GetLength(1)];

                    for (int h = 0; h < hps.Length; h++)
                    {
                        hps[h] = (ushort)Math.Min(current.HP + (ushort)(table.HP_GRADIENT * Controller.HP_GAIN[Math.Min(Math.Max((int)((h + 1) + hpDiffBase), 0), 11)] / 100), 9999);
                    }

                    for (int m = 0; m < mps.Length; m++)
                    {
                        mps[m] = (ushort)Math.Min(current.MP + (ushort)((((ushort)((current.Level + 1) * table.MP_GRADIENT / 10) - (ushort)(current.Level * table.MP_GRADIENT / 10)) * Controller.MP_GAIN[Math.Min(Math.Max((int)((m + 1) + mpDiffBase), 0), 11)]) / 100), 999);
                    }

                    //Randomly choosing the next level value
                    if (randomPointer > 3)
                    {
                        provider.GetBytes(rs);
                        randomPointer = 0;
                    }
                    int r = rs[randomPointer];
                    randomPointer++;
                    rngIdx = 1;
                    while (r > 0)
                    {
                        r -= Controller.RNG_LIST[(rngIdx - 1) / 8, (rngIdx - 1) % 8];

                        if (r >= 0)
                        {
                            rngIdx++;
                        }
                    }
                    rngIdx--;

                    while (Controller.RNG_LIST[rngIdx / 8, rngIdx % 8] == 0)
                    {
                        rngIdx++;
                    }

                    //Check if chosen value is safe
                    NodeLink chosenPath = null;

                    foreach (NodeLink child in current.ChildNodes)
                    {
                        if (child.Child.HP == hps[rngIdx / 8] && child.Child.MP == mps[rngIdx % 8])
                        {
                            valueIsSafe = true;
                            chosenPath = child;
                            break;
                        }
                    }

                    if (valueIsSafe)
                    {
                        switch (chosenPath.Quality)
                        {
                            case Quality.Bad:
                                valueIsSafe = false;
                                break;
                            case Quality.Questionable:
                                valueIsSafe = countResetsLevel < chosenPath.ResetTolerance;
                                break;
                        }
                    }

                    if (!valueIsSafe)
                    {
                        countResetsLevel++;
                    }
                    else
                    {
                        current = chosenPath.Child;
                    }
                }
                while (!valueIsSafe);

                if (shouldPrintLevel)
                {
                    output += String.Format("\nLevel {0} ({1}/{2}) ({3}&{4}) - {5} resets", current.Level, current.HP, current.MP, rngIdx / 8 + 1, rngIdx % 8 + 1, countResetsLevel);
                }

                countResets += countResetsLevel;
            }

            output += String.Format("\nMax HP/MP achieved with {0} resets", countResets);

            //MessageBox.Show(output);

            return countResets;
        }

        public int SimulateMaxSafeOnly(Node current)
        {
            int countResets = 0;

            System.Security.Cryptography.RNGCryptoServiceProvider provider = new System.Security.Cryptography.RNGCryptoServiceProvider();
            byte[] rs = new byte[4];
            byte randomPointer = 0;

            string output = String.Format("Level {0} ({1}/{2})", current.Level, current.HP, current.MP);
            bool shouldPrintLevel = false;

            while (current.Level < Controller.MAX_LEVEL)
            {
                bool valueIsSafe = false;

                int countResetsLevel = 0, rngIdx = 1;

                do
                {
                    HPMPGradientBase table = c.ActiveTree.Character.GetTable((byte)(current.Level + 1));

                    short hpDiffBase = (short)((short)(100 * (table.HP_BASE + (current.Level) * table.HP_GRADIENT) / current.HP) - 100),
                        mpDiffBase = (short)((short)(100 * (table.MP_BASE + (short)((current.Level) * table.MP_GRADIENT / 10)) / current.MP) - 100);

                    ushort[] hps = new ushort[Controller.RNG_LIST.GetLength(0)],
                        mps = new ushort[Controller.RNG_LIST.GetLength(1)];

                    for (int h = 0; h < hps.Length; h++)
                    {
                        hps[h] = (ushort)Math.Min(current.HP + (ushort)(table.HP_GRADIENT * Controller.HP_GAIN[Math.Min(Math.Max((int)((h + 1) + hpDiffBase), 0), 11)] / 100), 9999);
                    }

                    for (int m = 0; m < mps.Length; m++)
                    {
                        mps[m] = (ushort)Math.Min(current.MP + (ushort)((((ushort)((current.Level + 1) * table.MP_GRADIENT / 10) - (ushort)(current.Level * table.MP_GRADIENT / 10)) * Controller.MP_GAIN[Math.Min(Math.Max((int)((m + 1) + mpDiffBase), 0), 11)]) / 100), 999);
                    }

                    if (randomPointer > 3)
                    {
                        provider.GetBytes(rs);
                        randomPointer = 0;
                    }
                    int r = rs[randomPointer];
                    randomPointer++;
                    rngIdx = 1;
                    while (r > 0)
                    {
                        r -= Controller.RNG_LIST[(rngIdx - 1) / 8, (rngIdx - 1) % 8];

                        if (r >= 0)
                        {
                            rngIdx++;
                        }
                    }
                    rngIdx--;

                    while (Controller.RNG_LIST[rngIdx / 8, rngIdx % 8] == 0)
                    {
                        rngIdx++;
                    }

                    if (current.Level == c.ActiveTree.Character.SafetyLevel)
                    {
                        shouldPrintLevel = true;
                    }

                    foreach (NodeLink child in current.ChildNodes)
                    {
                        if (child.Child.HP == hps[rngIdx / 8] && child.Child.MP == mps[rngIdx % 8])
                        {
                            valueIsSafe = true;
                            current = child.Child;
                            break;
                        }
                    }

                    if (!valueIsSafe)
                    {
                        countResetsLevel++;
                    }
                }
                while (!valueIsSafe);

                if (shouldPrintLevel)
                {
                    output += String.Format("\nLevel {0} ({1}/{2}) ({3}&{4}) - {5} resets", current.Level, current.HP, current.MP, rngIdx / 8 + 1, rngIdx % 8 + 1, countResetsLevel);
                }

                countResets += countResetsLevel;
            }

            output += String.Format("\nMax HP/MP achieved with {0} resets", countResets);

            //MessageBox.Show(output);

            return countResets;
        }

        private void treePath_NodeMouseHover(object sender, TreeNodeMouseHoverEventArgs e)
        {
            if (e.Node.Tag is NodeLink)
            {
                NodeLink link = (NodeLink)e.Node.Tag;
                if (link.Quality == Quality.Questionable)
                {
                    nodeTip.Show(String.Format("Consider accepting value ONLY if you've reset less than {0:0.00} times, green is ALWAYS recommended", link.ResetTolerance), this, PointToClient(MousePosition));
                }
                else
                {
                    nodeTip.Hide(this);
                }
            }
            else
            {
                nodeTip.Hide(this);
            }
        }

        private void treePath_MouseMove(object sender, MouseEventArgs e)
        {
            if (treePath.GetNodeAt(treePath.PointToClient(MousePosition)) == null)
            {
                nodeTip.Hide(this);
            }
        }

        private void treePath_MouseLeave(object sender, EventArgs e)
        {
            nodeTip.Hide(this);
        }
    }
}
