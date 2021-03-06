﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using fNbt;

using Path = System.IO.Path;

namespace LightbarProg {
    /// <summary>
    /// The one class that handles the display and processing of the Phaser Programmer
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// The reference to the MCP2210 wrapper
        /// </summary>
        private Device d;
        private byte feedback_reg = 0;				// added jjc 10-9-15 for pass/fail feedback
        private bool writing = false;				// added jjc 10-9-15 for pass/fail feedback
        private bool feedback_ready = true;				// added jjc 10-9-15 for pass/fail feedback

        /// <summary>
        /// Initializes a new instance of the window.
        /// </summary>
        public MainWindow() {
            InitializeComponent();



            if(File.Exists("internal.txt")) {
                owDefault.IsChecked = true;			// force defaults over write if file exists
                owDefault.IsEnabled = false;        // force unchangeable      	
            }




            #region Make a new Timer to test for connection every second
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += delegate(object sender, EventArgs e) {
                Device dev = TryGetDevice();

                if(dev != null && dev.Connected) {
                    connLbl.Content = "Connected";
                    connImg.Source = ((Image)this.Resources["conn"]).Source;

                    inputLbl.Content = "Active Inputs:";

                    byte[] rxBuff;

                    try {
                        byte[] txBuff = new byte[] { 0, 1, 0, 2, 0, 3, 0, 4, 0, 5 };
                        dev.XferSize = 10;
                        rxBuff = dev.SpiTransfer(txBuff);

                        feedback_reg = (byte)((rxBuff[4] >> 6) & 3);		// if byte 4 buppter two bits = 3  succesfull transmisstion
                        //							  = 1|2 failure
                        //							  = 0 working   
                    } catch(DeviceErrorException ex) {
                        switch(ex.errCode) {
                            case -106:
                                MessageBox.Show("Issue interfacing with bar for input polling:  Could not write information to MCP2210.\nIs the bar connected?", "Error!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                                break;
                            default:
                                MessageBox.Show("Issue interfacing with bar for input polling: " + ex.errCode, "Error!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                                break;
                        }
                        return;
                    }

                    if(!feedback_ready) {
                        feedback_ready = feedback_reg == 0;
                    }

                    if(writing) {
                        if(feedback_reg == 1 || feedback_reg == 2) {
                            input.Content = string.Format("{0} {1} {2} {3} !  ERROR  !",
                                                      Convert.ToString(rxBuff[4], 2).PadLeft(8, '0'), Convert.ToString(rxBuff[5], 2).PadLeft(8, '0'),
                                                      Convert.ToString(rxBuff[2], 2).PadLeft(8, '0'), Convert.ToString(rxBuff[3], 2).PadLeft(8, '0'));
                        } else {
                            input.Content = string.Format("{0} {1} {2} {3} !Processing!",
                                                      Convert.ToString(rxBuff[4], 2).PadLeft(8, '0'), Convert.ToString(rxBuff[5], 2).PadLeft(8, '0'),
                                                      Convert.ToString(rxBuff[2], 2).PadLeft(8, '0'), Convert.ToString(rxBuff[3], 2).PadLeft(8, '0'));
                        }

                    } else {
                        input.Content = string.Format("MSB {0} {1} {2} {3} LSB",
                                                      Convert.ToString(rxBuff[4], 2).PadLeft(8, '0'), Convert.ToString(rxBuff[5], 2).PadLeft(8, '0'),
                                                      Convert.ToString(rxBuff[2], 2).PadLeft(8, '0'), Convert.ToString(rxBuff[3], 2).PadLeft(8, '0'));
                    }






                } else {
                    connLbl.Content = "Disconnected";
                    connImg.Source = ((Image)this.Resources["disconn"]).Source;

                    inputLbl.Content = "";
                    input.Content = "";

                    writing = false;
                }
            };
            timer.Interval = new TimeSpan(0, 0, 1); // <-- Means every second
            timer.Start();
            #endregion
        }

        /// <summary>
        /// Handles the Click event of the ReadBrowse control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void ReadBrowse_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog() { DefaultExt = ".bar.nbt", Filter = "Bar Files|*.bar.nbt", OverwritePrompt = true, DereferenceLinks = true, Title = "Browsing for Output File" };
            bool? result = dlg.ShowDialog(this);
            if(result == false) return;
            ReadBox.Text = dlg.FileName;
        }

        /// <summary>
        /// Handles the Click event of the WriteBrowse control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void WriteBrowse_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog() { DefaultExt = ".bar.nbt", Filter = "Bar Files|*.bar.nbt", Multiselect = false, CheckFileExists = true, DereferenceLinks = true, Title = "Browsing for Input File" };
            bool? result = dlg.ShowDialog(this);
            if(result == false) return;
            WriteBox.Text = dlg.FileName;
        }

        /// <summary>
        /// Handles processing of files being dragged onto the file text box.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DragEventArgs"/> instance containing the event data.</param>
        private void FileDragEnter(object sender, DragEventArgs e) {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach(string file in files) {
                if(file.EndsWith(".bar.nbt")) {
                    e.Effects = DragDropEffects.Link;
                    e.Handled = true;
                    return;
                }
            }
            e.Effects = DragDropEffects.None;
        }

        /// <summary>
        /// Handles processing of files being dropped onto the file text box.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DragEventArgs"/> instance containing the event data.</param>
        private void FileDragDrop(object sender, DragEventArgs e) {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            TextBox tb = sender as TextBox;
            if(tb != null && files != null && files.Length != 0) {
                foreach(string file in files) {
                    if(file.EndsWith(".bar.nbt")) {
                        tb.Text = file;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Reads pattern information off of the Phaser bar.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        private void ReadBar(object sender, MouseButtonEventArgs e) {
            Device dev = TryGetDevice();  // Try to get a handle on the MCP2210 on the CAN Breakout Box

            #region No Device found, let user know and stop now
            if(dev == null || !dev.Connected) {
                MessageBox.Show(this, "No bar was found.  Are you certain that one is connected?", "No Bar Connected", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            }
            #endregion
            #region No output file, let user know and stop now
            if(String.IsNullOrEmpty(ReadBox.Text)) {
                MessageBox.Show(this, "Please specify a destination to put the output file.", "No Destination Designated", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            }
            #endregion
            try {
                // Figure out where to dump the information
                string path = Path.GetFullPath(ReadBox.Text);

                NbtFile file = null;

                #region Get a handle to an existing bar file, if any.  Also gives null if Destination file is corrupted and user wants to overwrite it all.
                if(File.Exists(path)) {
                    try {
                        file = new NbtFile(path);
                    } catch(NbtFormatException) {
                        if(MessageBox.Show(this, "Destination file appears corrupt.  Is it alright to overwrite the whole file?", "Destination Corrupt", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                            return;
                        file = null;
                    } catch(EndOfStreamException) {
                        if(MessageBox.Show(this, "Destination file appears corrupt.  Is it alright to overwrite the whole file?", "Destination Corrupt", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                            return;
                        file = null;
                    } catch(InvalidCastException) {
                        if(MessageBox.Show(this, "Destination file appears corrupt.  Is it alright to overwrite the whole file?", "Destination Corrupt", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                            return;
                        file = null;
                    } catch(NullReferenceException) {
                        if(MessageBox.Show(this, "Destination file appears corrupt.  Is it alright to overwrite the whole file?", "Destination Corrupt", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                            return;
                        file = null;
                    }
                }
                #endregion

                #region Create a new file if one isn't supplied
                if(file == null) {
                    file = new NbtFile();
                    file.RootTag.Name = "root";

                    file.RootTag.Add(new NbtCompound("opts", new NbtTag[] { new NbtByte("size", 3), new NbtByte("tdop", 0), new NbtByte("can", 0), new NbtByte("cabt", 0), new NbtByte("cabl", 0) }));
                    file.RootTag.Add(new NbtCompound("ordr", new NbtTag[] { new NbtString("name", ""), new NbtString("num", ""), new NbtString("note", "Program imported by Lightbar Programmer") }));
                    file.RootTag.Add(new NbtCompound("pats"));
                    file.RootTag.Add(new NbtList("lite", NbtTagType.Compound));
                    file.RootTag.Add(new NbtList("soc", NbtTagType.Compound));
                    file.RootTag.Add(new NbtList("lens", NbtTagType.Compound));
                }
                #endregion

                #region Clear out the pattern tag, since we're overwriting it
                NbtCompound patts = file.RootTag.Get<NbtCompound>("pats");
                patts.Clear();
                #endregion

                #region Create a skeleton of a pattern tag
                foreach(string alpha in new string[] { "td", "lall", "rall", "ltai", "rtai", "cru", "cal", "emi", "l1", "l2", "l3", "l4", "l5", "tdp", "icl", "afl", "dcw", "dim", "traf" })
                    patts.Add(new NbtCompound(alpha));

                patts.Get<NbtCompound>("traf").AddRange(new NbtShort[] { new NbtShort("er1", 0), new NbtShort("er2", 0), new NbtShort("ctd", 0), new NbtShort("cwn", 0) });

                foreach(string alpha in new string[] { "td", "lall", "rall", "ltai", "rtai", "cru", "cal", "emi", "l1", "l2", "l3", "l4", "l5", "tdp", "icl", "afl", "dcw", "dim" })
                    patts.Get<NbtCompound>(alpha).AddRange(new NbtShort[] { new NbtShort("ef1", 0), new NbtShort("ef2", 0), new NbtShort("er1", 0), new NbtShort("er2", 0) });

                patts.Get<NbtCompound>("dim").Add(new NbtShort("dimp", 15));

                foreach(string alpha in new string[] { "l1", "l2", "l3", "l4", "l5", "tdp", "icl", "afl", "dcw" })
                    patts.Get<NbtCompound>(alpha).AddRange(new NbtShort[] { new NbtShort("pf1", 0), new NbtShort("pf2", 0), new NbtShort("pr1", 0), new NbtShort("pr2", 0) });

                patts.Get<NbtCompound>("traf").AddRange(new NbtShort[] { new NbtShort("patt", 0), new NbtShort("ctd2", 0), new NbtShort("cwn2", 0) });

                foreach(string alpha in new string[] { "l1", "l2", "l3", "l4", "l5", "tdp", "icl", "afl" }) {
                    patts.Get<NbtCompound>(alpha).Add(new NbtCompound("pat1", new NbtTag[] { new NbtShort("fcen", 0), new NbtShort("finb", 0), new NbtShort("foub", 0), new NbtShort("ffar", 0), new NbtShort("fcor", 0),
                                                                                             new NbtShort("rcen", 0), new NbtShort("rinb", 0), new NbtShort("roub", 0), new NbtShort("rfar", 0), new NbtShort("rcor", 0) }));
                    patts.Get<NbtCompound>(alpha).Add(new NbtCompound("pat2", new NbtTag[] { new NbtShort("fcen", 0), new NbtShort("finb", 0), new NbtShort("foub", 0), new NbtShort("ffar", 0), new NbtShort("fcor", 0),
                                                                                             new NbtShort("rcen", 0), new NbtShort("rinb", 0), new NbtShort("roub", 0), new NbtShort("rfar", 0), new NbtShort("rcor", 0) }));
                }

                patts.Add(new NbtIntArray("map", new int[20]));
                #endregion


                #region Create a reference for the PIC
                byte[] xferBuffer = new byte[768];
                using(MemoryStream xferBufferStream = new MemoryStream(xferBuffer))
                using(BarWriter writer = new BarWriter(xferBufferStream)) {
                    writer.Write(new byte[] { 0, 10 }); // Write command
                    for(ushort i = 11; i < 394; i++) {
                        writer.Write(i); // Fill with bytes for PIC reference
                    }
                }
                #endregion

                // Perform the transfer
                dev.XferSize = 768;
                byte[] rxBuffer = dev.SpiTransfer(xferBuffer);

                #region Log it
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Read Op @ {0:G}\n\nSent: [{1} bytes]\n", DateTime.Now, xferBuffer.Length);
                for(short i = 0; i < xferBuffer.Length; i++) {
                    sb.Append(xferBuffer[i]);
                    if(i % 2 == 1) sb.Append("\n");
                    else sb.Append(" ");
                }
                sb.AppendFormat("\n\nRecieved: [{0} bytes]\n", rxBuffer.Length);
                for(short i = 0; i < rxBuffer.Length; i++) {
                    sb.Append(rxBuffer[i]);
                    if(i % 2 == 1) sb.Append("\n");
                    else sb.Append(" ");
                }
                sb.Append("\n<End of transfer>\n\n");

                File.AppendAllText("log.txt", sb.ToString());
                #endregion

                #region Parsing
                using(MemoryStream rxBufferStream = new MemoryStream(rxBuffer))
                using(BarReader reader = new BarReader(rxBufferStream)) {
                    NbtCompound patt = patts, func;
                    short val = 0;

                    reader.ReadShort();

                    foreach(string alpha in new string[] { "td", "lall", "rall", "l1", "l2", "l3", "l4", "l5", "dcw", "tdp", "afl", "icl", "ltai", "rtai", "cru", "cal", "emi", "dim" }) {
                        func = patt.Get<NbtCompound>(alpha); // Read all the enables from the byte buffer
                        foreach(string beta in new string[] { "ef1", "ef2", "er1", "er2" }) {
                            func.Get<NbtShort>(beta).Value = reader.ReadShort();
                        }
                    }

                    func = patt.Get<NbtCompound>("traf");
                    foreach(string beta in new string[] { "er1", "er2" }) {
                        func.Get<NbtShort>(beta).Value = reader.ReadShort(); // Read the traffic director's enables
                    }

                    foreach(string alpha in new string[] { "l1", "l2", "l3", "l4", "l5", "dcw", "tdp", "icl", "afl" }) {
                        func = patt.Get<NbtCompound>(alpha); // Read the phases
                        foreach(string beta in new string[] { "pf1", "pf2", "pr1", "pr2" }) {
                            func.Get<NbtShort>(beta).Value = reader.ReadShort();
                        }
                    }

                    NbtCompound patternColor;
                    foreach(string alpha in new string[] { "l1", "l2", "l3", "l4", "l5", "afl", "tdp", "icl" }) {
                        func = patt.Get<NbtCompound>(alpha); // Read patterns
                        foreach(string beta in new string[] { "pat1", "pat2" }) {
                            patternColor = func.Get<NbtCompound>(beta);
                            foreach(string charlie in new string[] { "fcen", "finb", "foub", "ffar", "fcor", "rcen", "rinb", "roub", "rfar", "rcor" }) {
                                patternColor.Get<NbtShort>(charlie).Value = reader.ReadShort();
                            }
                        }
                    }

                    func = patt.Get<NbtCompound>("traf");
                    func.Get<NbtShort>("patt").Value = reader.ReadShort();
                    for(byte alpha = 0; alpha < 2; alpha++) { // Traffic director's patterns 3x (left, right, center) (they should be the same anyway, ask James why they're separate)
                        reader.ReadShort();  // Eat two Shorts to discard extra Traffic Director patterns.
                    }
                    func.Get<NbtShort>("ctd").Value = reader.ReadShort(); // Cycles TD value
                    func.Get<NbtShort>("cwn").Value = reader.ReadShort(); // Cycles Warn value

                    func = patt.Get<NbtCompound>("dim");
                    func.Get<NbtShort>("dimp").Value = reader.ReadShort(); // Dim Percentage, ignored last I checked

                    val = reader.ReadShort();
                    if(val != 0) {
                        patt.Add(new NbtByte("prog", (byte)val)); // Preset program number
                    }

                    int[] mapping = patt.Get<NbtIntArray>("map").Value; // Then read out the input map.
                    for(byte alpha = 0; alpha < 20; alpha++) {
                        mapping[alpha] = reader.ReadInt();
                    }
                }
                #endregion


                file.SaveToFile(path, NbtCompression.None); // Save file
                if(rxBuffer[2] == 0 && rxBuffer[3] == 255 && rxBuffer[4] == 0 && rxBuffer[5] == 255 && rxBuffer[6] == 0 && rxBuffer[7] == 255 && rxBuffer[8] == 0 && rxBuffer[9] == 255)
                    MessageBox.Show(this, "Error bar is not ready – try again in a little bit", "Bar Error", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                else
                    MessageBox.Show(this, "Received data", "Success", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK);


                //                MessageBox.Show(this, "Read operation completed.", "Done", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK); // Transfer completed
                return;
            } catch(ArgumentException) {
                MessageBox.Show(this, "Please don't use any invalid characters in the path.", "Invalid Destination Designated", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            } catch(NotSupportedException) {
                MessageBox.Show(this, "Please don't use any invalid characters in the path.", "Invalid Destination Designated", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            } catch(PathTooLongException) {
                MessageBox.Show(this, "The specified output path is too long for Windows to use.", "Invalid Destination Designated", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            } catch(System.Security.SecurityException) {
                MessageBox.Show(this, "You do not have permission to write to the specified output path.", "Invalid Destination Designated", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            } catch(DeviceErrorException ex) {
                switch(ex.errCode) {
                    case -2:
                    case -8:
                    case -10:
                        MessageBox.Show(this, "Cannot communicate with bar.  Wait a few seconds and try again. (" + ex.errCode + ")", "Bar Error", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                        break;
                    case -9:
                        MessageBox.Show(this, "Cannot communicate with bar.  The communication channel might be damaged. (" + ex.errCode + ")", "Bar Error", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                        break;
                    case -101:
                        MessageBox.Show(this, "Cannot communicate with bar.  Is it connected? (" + ex.errCode + ")", "Bar Error", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                        break;
                    default:
                        MessageBox.Show(this, "Unknown error communicating with bar.  Wait a few seconds and try again. (" + ex.errCode + ")", "Bar Error", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                        break;
                }
            }
        }

        /// <summary>
        /// Writes pattern information onto the Phaser bar.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        private void WriteBar(object sender, MouseButtonEventArgs e) {

            if(writing) return; 			// only write once
            if(feedback_reg != 0) {
                MessageBox.Show("Please wait a bit, the information from the previous transmission is still in the buffer.", "Hold On", MessageBoxButton.OK, MessageBoxImage.None);
                return;
            }

            Device dev = TryGetDevice();  // Try to get a handle on the MCP2210 on the CAN Breakout Box


            #region No Device found, let user know and stop now
            if(dev == null || !dev.Connected) {
                MessageBox.Show(this, "No bar was found.  Are you certain that one is connected?", "No Bar Connected", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            }
            #endregion

            if(facDefault.IsChecked.Value) {									// added JJC 10-7-15 
                byte[] xferBuffer = new byte[] { 4, 0, 0, 0 };					// send a command to re-set bar defaults 0x40
                dev.XferSize = 4;
                byte[] rxBuffer = dev.SpiTransfer(xferBuffer);
                // perform checks on rxBuffer – ie if(rxBuffer[2] != 4) MessageBox.Show(this, “Problem!”...

                writing = true;

                StartFeedbackTimer();
                return;
            }

            #region No input file, let user know and stop now
            if(String.IsNullOrEmpty(WriteBox.Text)) {
                MessageBox.Show(this, "Please specify a source file.", "No Source Designated", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            }
            #endregion
            try {
                // Figure out where to pull the information
                string path = Path.GetFullPath(WriteBox.Text);

                NbtFile file = null;

                #region Stop if no file exists at specified path
                if(!File.Exists(path)) {
                    MessageBox.Show(this, "The file name you indicated does not exist.", "Invalid Source Designated", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                    return;
                }
                #endregion

                // Attempt to read the file
                file = new NbtFile(path);

                // Prepare a buffer of bytes to send
                byte[] xferBuffer = new byte[768];

                if(owDefault.IsChecked.Value == true) {				// send command to write to defaults as well james
                    xferBuffer[765] = 1;
                }

                writing = true;	// writing data  jjc 
                //feedback_ready = true;// writing data  jjc

                using(MemoryStream xferBufferStream = new MemoryStream(xferBuffer))
                using(BarWriter writer = new BarWriter(xferBufferStream)) {
                    writer.Write(new byte[] { 2, 0 }); // Write command

                    NbtCompound patt = file.RootTag.Get<NbtCompound>("pats"), func;
                    short val = 0;
                    foreach(string alpha in new string[] { "td", "lall", "rall", "l1", "l2", "l3", "l4", "l5", "dcw", "tdp", "afl", "icl", "ltai", "rtai", "cru", "cal", "emi", "dim" }) {
                        func = patt.Get<NbtCompound>(alpha); // Add all the enables to the byte buffer
                        foreach(string beta in new string[] { "ef1", "ef2", "er1", "er2" }) {
                            val = func.Get<NbtShort>(beta).Value;

                            writer.Write(val);
                        }
                    }

                    func = patt.Get<NbtCompound>("traf");
                    foreach(string beta in new string[] { "er1", "er2" }) {
                        val = func.Get<NbtShort>(beta).Value; // Add the traffic director's enables

                        writer.Write(val);
                    }

                    foreach(string alpha in new string[] { "l1", "l2", "l3", "l4", "l5", "dcw", "tdp", "icl", "afl" }) {
                        func = patt.Get<NbtCompound>(alpha); // Add the phases
                        foreach(string beta in new string[] { "pf1", "pf2", "pr1", "pr2" }) {
                            val = func.Get<NbtShort>(beta).Value;

                            writer.Write(val);
                        }
                    }

                    NbtCompound patternColor;
                    foreach(string alpha in new string[] { "l1", "l2", "l3", "l4", "l5", "afl", "tdp", "icl" }) {
                        func = patt.Get<NbtCompound>(alpha); // Add patterns
                        foreach(string beta in new string[] { "pat1", "pat2" }) {
                            patternColor = func.Get<NbtCompound>(beta);
                            foreach(string charlie in new string[] { "fcen", "finb", "foub", "ffar", "fcor", "rcen", "rinb", "roub", "rfar", "rcor" }) {
                                val = patternColor.Get<NbtShort>(charlie).Value;

                                writer.Write(val);
                            }
                        }
                    }

                    func = patt.Get<NbtCompound>("traf");
                    val = func.Get<NbtShort>("patt").Value;
                    for(byte alpha = 0; alpha < 3; alpha++) { // Add traffic director's patterns 3x (left, right, center) (they should be the same anyway, ask James why they're separate)
                        writer.Write(val);
                    }
                    if(func.Contains("ctd"))
                        writer.Write(func.Get<NbtShort>("ctd").Value); // Cycles TD value
                    else
                        writer.Write((short)0);
                    if(func.Contains("cwn"))
                        writer.Write(func.Get<NbtShort>("cwn").Value); // Cycles Warn value
                    else
                        writer.Write((short)0);

                    func = patt.Get<NbtCompound>("dim");
                    val = func.Get<NbtShort>("dimp").Value; // Dim Percentage, ignored last I checked
                    writer.Write(val);

                    if(patt.Contains("prog")) {
                        val = patt.Get<NbtByte>("prog").ShortValue; // Preset program number
                    } else {
                        val = 0; // Not a preset program
                    }
                    writer.Write(val);

                    int[] mapping = patt.Get<NbtIntArray>("map").Value; // Then put in the input map.
                    for(byte alpha = 0; alpha < 20; alpha++) {
                        writer.Write(mapping[alpha]);
                    }
                }

                // Perform the transfer
                dev.XferSize = 768;
                byte[] rxBuffer = dev.SpiTransfer(xferBuffer);

                #region Log it
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Write Op @ {0:G}\n\nSent: [{1} bytes]\n", DateTime.Now, xferBuffer.Length);
                for(short i = 0; i < xferBuffer.Length; i++) {
                    sb.Append(xferBuffer[i]);
                    if(i % 2 == 1) sb.Append("\n");
                    else sb.Append(" ");
                }
                sb.AppendFormat("\n\nRecieved: [{0} bytes]\n", rxBuffer.Length);
                for(short i = 0; i < rxBuffer.Length; i++) {
                    sb.Append(rxBuffer[i]);
                    if(i % 2 == 1) sb.Append("\n");
                    else sb.Append(" ");
                }
                sb.Append("\n<End of transfer>\n\n");

                File.AppendAllText("log.txt", sb.ToString());
                #endregion

                #region Verify integrity of connection
                if(rxBuffer[2] != 2 || rxBuffer[3] != 0) {
                    MessageBox.Show(this, "Write operation complete, but data integrity is not verifiable.  Another attempt is recommended.", "Complete (With Complications)", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                    return;
                }
                byte upper = 2, lower = 1;
                ushort addr = 4;
                while(addr < 768) {
                    if(rxBuffer[addr++] != upper) {
                        MessageBox.Show(this, "Write operation complete, but data integrity is not verifiable.  Another attempt is recommended.", "Complete (With Complications)", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                        return;
                    }
                    if(rxBuffer[addr++] != lower) {
                        MessageBox.Show(this, "Write operation complete, but data integrity is not verifiable.  Another attempt is recommended.", "Complete (With Complications)", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                        return;
                    }
                    if(lower == 255) {
                        upper++;
                        lower = 0;
                    } else {
                        lower++;
                    }
                }

                #endregion


                StartFeedbackTimer();

                //              MessageBox.Show(this, "Write operation complete.", "Complete", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK); // Transfer completed
                return;
            } catch(ArgumentException) {
                MessageBox.Show(this, "Please don't use any invalid characters in the path.", "Invalid Source Designated", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            } catch(NotSupportedException) {
                MessageBox.Show(this, "Please don't use any invalid characters in the path.", "Invalid Source Designated", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            } catch(PathTooLongException) {
                MessageBox.Show(this, "The specified source path is too long for Windows to use.", "Invalid Source Designated", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            } catch(System.Security.SecurityException) {
                MessageBox.Show(this, "You do not have permission to read from the specified source path.", "Invalid Source Designated", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            } catch(NbtFormatException) {
                MessageBox.Show(this, "Source file appears corrupt.", "Source Corrupt", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            } catch(EndOfStreamException) {
                MessageBox.Show(this, "Source file appears corrupt.", "Source Corrupt", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            } catch(InvalidCastException) {
                MessageBox.Show(this, "Source file appears corrupt.", "Source Corrupt", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            } catch(NullReferenceException) {
                MessageBox.Show(this, "Source file appears corrupt.", "Source Corrupt", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                return;
            } catch(DeviceErrorException ex) {
                switch(ex.errCode) {
                    case -2:
                    case -8:
                    case -10:
                        MessageBox.Show(this, "Cannot communicate with bar.  Wait a few seconds and try again. (" + ex.errCode + ")", "Bar Error", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                        break;
                    case -9:
                        MessageBox.Show(this, "Cannot communicate with bar.  The communication channel might be damaged. (" + ex.errCode + ")", "Bar Error", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                        break;
                    case -101:
                        MessageBox.Show(this, "Cannot communicate with bar.  Is it connected? (" + ex.errCode + ")", "Bar Error", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                        break;
                    default:
                        MessageBox.Show(this, "Unknown error communicating with bar.  Wait a few seconds and try again. (" + ex.errCode + ")", "Bar Error", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
                        break;
                }
            }
        }

        private void StartFeedbackTimer() {
            #region Make a new Timer to test to check for process complete
            System.Windows.Threading.DispatcherTimer feedbackTimer = new System.Windows.Threading.DispatcherTimer();
            feedbackTimer.Tick += delegate(object sent, EventArgs ea) {

                if(!writing) { // Bar disconnected, cancel this timer.
                    feedback_ready = true;
                    feedbackTimer.Stop();
                    return;
                }

                if((feedback_reg == 3) && feedback_ready) {
                    writing = false; // Return the bottom ticker to original state
                    feedback_ready = false; // Make sure program doesn't care about more feedback_reg values
                    feedbackTimer.Stop(); // Stop checking, we don’t care about the value anymore
                    MessageBox.Show(this, "Write operation successful.", "Complete", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK); // Transfer completed

                } else if((feedback_reg == 2) && feedback_ready) {
                    writing = false;
                    feedback_ready = false; // Make sure program doesn't care about more feedback_reg values
                    feedbackTimer.Stop();
                    MessageBox.Show(this, "Fail 2 check can cable, check bar power", "Fail", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK);
                } else if((feedback_reg == 1) && feedback_ready) {
                    writing = false;
                    feedback_ready = false; // Make sure program doesn't care about more feedback_reg values
                    feedbackTimer.Stop();
                    MessageBox.Show(this, "Fail 1 check can cable, check bar power", "Fail", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK);
                }
                // Do nothing if zero
            };
            feedbackTimer.Interval = new TimeSpan(0, 0, 1); // Every second
            feedbackTimer.Start();
            #endregion
        }

        /// <summary>
        /// Handles the Closing event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            Device dev = TryGetDevice();
            if(dev != null) dev.Dispose();
        }

        /// <summary>
        /// Attempts to get a handle for an MCP2210.
        /// </summary>
        /// <returns>A wrapper for the MCP2210, if one is connected.</returns>
        private Device TryGetDevice() {
            if(d != null) {
                if(d.Connected)
                    return d;
                else {
                    d.Dispose();
                    d = null;
                }
            }

            try {
                d = new Device();
            } catch(DeviceErrorException ex) {
                if(ex.errCode == -101) {
                    if(d != null) d.Dispose();
                    try {
                        d = new Device(0x04D8, 0x00DE);
                        d.ProductID = 0xF2CF;
                        d.ProdDescriptor = "Light Bar Breakout Box";
                        d.Manufacturer = "Star Headlight & Lantern Co.";
                        d.SetAllSpiSettings(100000, 0xFFFF, 0xFFFE, 100, 1, 1, 768, 1, true);
                        d.SetGpioConfig(new byte[] { 0x1, 0x0, 0x0, 0x2, 0x0, 0x0, 0x0, 0x0, 0x0 }, 0, 0, true);
                    } catch(DeviceErrorException ex2) {
                        if(ex2.errCode == -101) {
                            if(d != null) d.Dispose();
                            d = null;
                        }
                    }
                }
            }
            return d;
        }



        void show_list(object sender, MouseButtonEventArgs e) {
            MessageBox.Show(" LIVE INPUT DEBUG TOOL:\n" +
                            " MSB 000000000   000UTSRQ   PONMLKJI   HGFEDCBA   LSB\n" +
                            "A = INPUT 1 (TAKE DOWN)         L = INPUT 12 (Cruise)\n" +
                            "B = INPUT 2 (PRIORITY 1)          M = NA (PATTERN) \n" +
                            "C = INPUT 3 (PRIORITY 2)          N = INPUT 13 (AF)\n" +
                            "D = INPUT 4 (PRIORITY 3)          O = INPUT 14 (TURN L)\n" +
                            "E = INPUT 5 (TRAFFIC LEFT)       P = INPUT 15 (TURN R)\n" +
                            "F = INPUT 6 (TRAFFIC RIGHT)     Q = INPUT 16 (TAIL)\n" +
                            "G = INPUT 7 (L ALLEY)                 R = INPUT 17 (CAL)\n" +
                            "H = INPUT 8  (R ALLEY)               S = INPUT 18 (L4)\n" +
                            "I = INPUT 9  (ICL)	                   T = INPUT 19 (L5)\n" +
                            "J = INPUT 10 (DIM) 	    U = INPUT 20 (EMITTER)\n" +
                            "K = INPUT 11 (TDF)", "function list", MessageBoxButton.OK, MessageBoxImage.Information);






        }
    }

    /// <summary>
    /// Class that handles writing to the Phaser bar.
    /// </summary>
    public class BarWriter : IDisposable {
        /// <summary>
        /// The output stream
        /// </summary>
        protected Stream outStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="BarWriter"/> class.
        /// </summary>
        /// <param name="output">The output stream to use.</param>
        public BarWriter(Stream output) {
            outStream = output;
        }

        /// <summary>
        /// Writes the specified bytes to the stream.
        /// </summary>
        public void Write(byte[] bytes) {
            outStream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Writes the specified byte to the stream.
        /// </summary>
        public void Write(byte b) {
            outStream.WriteByte(b);
        }

        /// <summary>
        /// Writes the specified unsigned short to the stream.
        /// </summary>
        public void Write(ushort s) {
            outStream.WriteByte((byte)((s >> 8) & 0xFF));
            outStream.WriteByte((byte)(s & 0xFF));
        }

        /// <summary>
        /// Writes the specified short to the stream.
        /// </summary>
        public void Write(short s) {
            outStream.WriteByte((byte)((s >> 8) & 0xFF));
            outStream.WriteByte((byte)(s & 0xFF));
        }

        /// <summary>
        /// Writes the specified integer to the stream.
        /// </summary>
        public void Write(int i) {
            outStream.WriteByte((byte)((i >> 24) & 0xFF));
            outStream.WriteByte((byte)((i >> 16) & 0xFF));
            outStream.WriteByte((byte)((i >> 8) & 0xFF));
            outStream.WriteByte((byte)(i & 0xFF));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            outStream.Dispose();
        }
    }

    /// <summary>
    /// Class that handles reading from to the Phaser bar.
    /// </summary>
    public class BarReader : IDisposable {
        /// <summary>
        /// The input stream
        /// </summary>
        protected Stream inStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="BarReader"/> class.
        /// </summary>
        /// <param name="output">The input stream to use.</param>
        public BarReader(Stream input) {
            inStream = input;
        }

        /// <summary>
        /// Reads an integer off the stream.
        /// </summary>
        /// <returns>The integer that was read.</returns>
        public int ReadInt() {
            int rtn = 0;
            rtn |= (int)(((int)ReadByte()) << 24);
            rtn |= (int)(((int)ReadByte()) << 16);
            rtn |= (int)(((int)ReadByte()) << 8);
            rtn |= (int)ReadByte();
            return rtn;
        }

        /// <summary>
        /// Reads a short off the stream.
        /// </summary>
        /// <returns>The short that was read.</returns>
        public short ReadShort() {
            short rtn = 0;
            rtn |= (short)(ReadByte() << 8);
            rtn |= (short)ReadByte();
            return rtn;
        }

        /// <summary>
        /// Reads a byte off the stream.
        /// </summary>
        /// <returns>The byte that was read.</returns>
        public byte ReadByte() {
            int val = inStream.ReadByte();
            if(val == -1) throw new EndOfStreamException();
            return (byte)val;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            inStream.Dispose();
        }
    }
}
