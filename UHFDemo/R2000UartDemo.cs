using System;
using System.Data;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using CustomControl;
using MySql.Data.MySqlClient;
using Reader;

namespace UHFDemo
{
    public partial class R2000UartDemo : Form
    {
        //Record quick poll antenna parameter.
        private readonly byte[] m_btAryData = new byte[10];
        private readonly InventoryBuffer m_curInventoryBuffer = new InventoryBuffer();
        private readonly OperateTagBuffer m_curOperateTagBuffer = new OperateTagBuffer();
        private readonly OperateTagISO18000Buffer m_curOperateTagISO18000Buffer = new OperateTagISO18000Buffer();

        private readonly ReaderSetting m_curSetting = new ReaderSetting();

        //Frequency of list updating.
        private readonly int m_nRealRate = 20;

        //ISO18000 tag continuously inventory mark.
        private bool m_bContinue;

        //Whether display the serial monitoring data.
        private bool m_bDisplayLog;

        //Before inventory, you need to set working antenna to identify whether the inventory operation is executing.
        private bool m_bInventory;

        //Real time inventory locking operation.
        private bool m_bLockTab;

        //Identify whether reckon the command execution time, and the current inventory command needs to reckon time.
        private bool m_bReckonTime = false;

        //Record the number of ISO18000 tag's written characters.
        private int m_nBytes;

        //Record the number of ISO18000 tag have been written loop time.
        private int m_nLoopedTimes;

        //Record the number of ISO18000 tag written loop time.
        private int m_nLoopTimes;

        private int m_nReceiveFlag;

        private int m_nSwitchTime;

        //Record the total number of quick poll times.
        private int m_nSwitchTotal;

        //Real time inventory times.
        private int m_nTotal;
        private ReaderMethod reader;

        public R2000UartDemo()
        {
            InitializeComponent();
        }

        private void R2000UartDemo_Load(object sender, EventArgs e)
        {
            //The real example of accessing reader initialization.
            reader = new ReaderMethod();

            //Callback function
            reader.AnalyCallback = AnalyData;
            reader.ReceiveCallback = ReceiveData;
            reader.SendCallback = SendData;

            //Set the validity of interface element.
            gbRS232.Enabled = false;
            gbTcpIp.Enabled = false;
            SetFormEnable(false);
            rdbRS232.Checked = true;

            //Initialization connect the default configuration of reader.
            cmbComPort.SelectedIndex = 0;
            cmbBaudrate.SelectedIndex = 1;
            ipIpServer.IpAddressStr = "192.168.0.178";
            txtTcpPort.Text = "4001";


            rdbInventoryRealTag_CheckedChanged(sender, e);
            cmbSession.SelectedIndex = 0;
            cmbTarget.SelectedIndex = 0;
            cmbReturnLossFreq.SelectedIndex = 33;
            if (cbUserDefineFreq.Checked)
            {
                groupBox21.Enabled = false;
                groupBox23.Enabled = true;
            }
            else
            {
                groupBox21.Enabled = true;
                groupBox23.Enabled = false;
            }

            ;
        }

        private void ReceiveData(byte[] btAryReceiveData)
        {
            if (m_bDisplayLog)
            {
                var strLog = CCommondMethod.ByteArrayToString(btAryReceiveData, 0, btAryReceiveData.Length);

                WriteLog(lrtxtDataTran, strLog, 1);
            }
        }

        private void SendData(byte[] btArySendData)
        {
            if (m_bDisplayLog)
            {
                var strLog = CCommondMethod.ByteArrayToString(btArySendData, 0, btArySendData.Length);

                WriteLog(lrtxtDataTran, strLog, 0);
            }
        }

        private void AnalyData(MessageTran msgTran)
        {
            m_nReceiveFlag = 0;
            if (msgTran.PacketType != 0xA0) return;
            switch (msgTran.Cmd)
            {
                case 0x69:
                    ProcessSetProfile(msgTran);
                    break;
                case 0x6A:
                    ProcessGetProfile(msgTran);
                    break;
                case 0x71:
                    ProcessSetUartBaudrate(msgTran);
                    break;
                case 0x72:
                    ProcessGetFirmwareVersion(msgTran);
                    break;
                case 0x73:
                    ProcessSetReadAddress(msgTran);
                    break;
                case 0x74:
                    ProcessSetWorkAntenna(msgTran);
                    break;
                case 0x75:
                    ProcessGetWorkAntenna(msgTran);
                    break;
                case 0x76:
                    ProcessSetOutputPower(msgTran);
                    break;
                case 0x77:
                    ProcessGetOutputPower(msgTran);
                    break;
                case 0x78:
                    ProcessSetFrequencyRegion(msgTran);
                    break;
                case 0x79:
                    ProcessGetFrequencyRegion(msgTran);
                    break;
                case 0x7A:
                    ProcessSetBeeperMode(msgTran);
                    break;
                case 0x7B:
                    ProcessGetReaderTemperature(msgTran);
                    break;
                case 0x7C:
                    ProcessSetDrmMode(msgTran);
                    break;
                case 0x7D:
                    ProcessGetDrmMode(msgTran);
                    break;
                case 0x7E:
                    ProcessGetImpedanceMatch(msgTran);
                    break;
                case 0x60:
                    ProcessReadGpioValue(msgTran);
                    break;
                case 0x61:
                    ProcessWriteGpioValue(msgTran);
                    break;
                case 0x62:
                    ProcessSetAntDetector(msgTran);
                    break;
                case 0x63:
                    ProcessGetAntDetector(msgTran);
                    break;
                case 0x67:
                    ProcessSetReaderIdentifier(msgTran);
                    break;
                case 0x68:
                    ProcessGetReaderIdentifier(msgTran);
                    break;

                case 0x80:
                    ProcessInventory(msgTran);
                    break;
                case 0x81:
                    ProcessReadTag(msgTran);
                    break;
                case 0x82:
                    ProcessWriteTag(msgTran);
                    break;
                case 0x83:
                    ProcessLockTag(msgTran);
                    break;
                case 0x84:
                    ProcessKillTag(msgTran);
                    break;
                case 0x85:
                    ProcessSetAccessEpcMatch(msgTran);
                    break;
                case 0x86:
                    ProcessGetAccessEpcMatch(msgTran);
                    break;

                case 0x89:
                case 0x8B:
                    ProcessInventoryReal(msgTran);
                    break;
                case 0x8A:
                    ProcessFastSwitch(msgTran);
                    break;
                case 0x8D:
                    ProcessSetMonzaStatus(msgTran);
                    break;
                case 0x8E:
                    ProcessGetMonzaStatus(msgTran);
                    break;
                case 0x90:
                    ProcessGetInventoryBuffer(msgTran);
                    break;
                case 0x91:
                    ProcessGetAndResetInventoryBuffer(msgTran);
                    break;
                case 0x92:
                    ProcessGetInventoryBufferTagCount(msgTran);
                    break;
                case 0x93:
                    ProcessResetInventoryBuffer(msgTran);
                    break;
                case 0xb0:
                    ProcessInventoryISO18000(msgTran);
                    break;
                case 0xb1:
                    ProcessReadTagISO18000(msgTran);
                    break;
                case 0xb2:
                    ProcessWriteTagISO18000(msgTran);
                    break;
                case 0xb3:
                    ProcessLockTagISO18000(msgTran);
                    break;
                case 0xb4:
                    ProcessQueryISO18000(msgTran);
                    break;
            }
        }

        private void WriteLog(LogRichTextBox logRichTxt, string strLog, int nType)
        {
            if (InvokeRequired)
            {
                WriteLogUnSafe InvokeWriteLog = WriteLog;
                Invoke(InvokeWriteLog, logRichTxt, strLog, nType);
            }
            else
            {
                if (nType == 0)
                    logRichTxt.AppendTextEx(strLog, Color.Indigo);
                else
                    logRichTxt.AppendTextEx(strLog, Color.Red);

                if (ckClearOperationRec.Checked)
                    if (logRichTxt.Lines.Length > 50)
                        logRichTxt.Clear();

                logRichTxt.Select(logRichTxt.TextLength, 0);
                logRichTxt.ScrollToCaret();
            }
        }

        private void RefreshInventory(byte btCmd)
        {
            if (InvokeRequired)
            {
                var InvokeRefresh = new RefreshInventoryUnsafe(RefreshInventory);
                Invoke(InvokeRefresh, btCmd);
            }
            else
            {
                switch (btCmd)
                {
                    case 0x80:
                    {
                        ledBuffer1.Text = m_curInventoryBuffer.nTagCount.ToString();
                        ledBuffer2.Text = m_curInventoryBuffer.nReadRate.ToString();

                        var ts = m_curInventoryBuffer.dtEndInventory - m_curInventoryBuffer.dtStartInventory;
                        ledBuffer5.Text = (ts.Minutes * 60 * 1000 + ts.Seconds * 1000 + ts.Milliseconds).ToString();
                        var nTotalRead = 0;
                        foreach (var nTemp in m_curInventoryBuffer.lTotalRead) nTotalRead += nTemp;
                        ledBuffer4.Text = nTotalRead.ToString();
                        var commandDuration = 0;
                        if (m_curInventoryBuffer.nReadRate > 0)
                            commandDuration = m_curInventoryBuffer.nDataCount * 1000 / m_curInventoryBuffer.nReadRate;
                        ledBuffer3.Text = commandDuration.ToString();
                        var currentAntDisplay = 0;
                        currentAntDisplay = m_curInventoryBuffer.nCurrentAnt + 1;
                    }
                        break;
                    case 0x90:
                    case 0x91:
                    {
                        var nCount = lvBufferList.Items.Count;
                        var nLength = m_curInventoryBuffer.dtTagTable.Rows.Count;
                        var row = m_curInventoryBuffer.dtTagTable.Rows[nLength - 1];

                        var item = new ListViewItem();
                        item.Text = (nCount + 1).ToString();
                        item.SubItems.Add(row[0].ToString());
                        item.SubItems.Add(row[1].ToString());
                        item.SubItems.Add(row[2].ToString());
                        item.SubItems.Add(row[3].ToString());

                        var strTemp = Convert.ToInt32(row[4].ToString()) - 129 + "dBm";
                        item.SubItems.Add(strTemp);
                        var byTemp = Convert.ToByte(row[4]);
                        /*   if (byTemp > 0x50)
                           {
                               item.BackColor = Color.PowderBlue;
                           }
                           else if (byTemp < 0x30)
                           {
                               item.BackColor = Color.LemonChiffon;
                           } */

                        item.SubItems.Add(row[5].ToString());

                        lvBufferList.Items.Add(item);
                        lvBufferList.Items[nCount].EnsureVisible();

                        labelBufferTagCount.Text = "Tag List: " + m_curInventoryBuffer.nTagCount + " ";
                    }
                        break;
                    case 0x92:
                    {
                    }
                        break;
                    case 0x93:
                    {
                    }
                        break;
                }
            }
        }

        private void RefreshOpTag(byte btCmd)
        {
            if (InvokeRequired)
            {
                var InvokeRefresh = new RefreshOpTagUnsafe(RefreshOpTag);
                Invoke(InvokeRefresh, btCmd);
            }
            else
            {
                switch (btCmd)
                {
                    case 0x81:
                    case 0x82:
                    case 0x83:
                    case 0x84:
                    {
                        var nCount = ltvOperate.Items.Count;
                        var nLength = m_curOperateTagBuffer.dtTagTable.Rows.Count;

                        var row = m_curOperateTagBuffer.dtTagTable.Rows[nLength - 1];

                        var item = new ListViewItem();
                        item.Text = (nCount + 1).ToString();
                        item.SubItems.Add(row[0].ToString());
                        item.SubItems.Add(row[1].ToString());
                        item.SubItems.Add(row[2].ToString());
                        item.SubItems.Add(row[3].ToString());
                        item.SubItems.Add(row[4].ToString());
                        item.SubItems.Add(row[5].ToString());
                        item.SubItems.Add(row[6].ToString());

                        ltvOperate.Items.Add(item);
                    }
                        break;
                    case 0x86:
                    {
                        txtAccessEpcMatch.Text = m_curOperateTagBuffer.strAccessEpcMatch;
                    }
                        break;
                }
            }
        }

        private void RefreshInventoryReal(byte btCmd)
        {
            if (InvokeRequired)
            {
                var InvokeRefresh = new RefreshInventoryRealUnsafe(RefreshInventoryReal);
                Invoke(InvokeRefresh, btCmd);
            }
            else
            {
                switch (btCmd)
                {
                    case 0x89:
                    case 0x8B:
                    {
                        var nTagCount = m_curInventoryBuffer.dtTagTable.Rows.Count;
                        var nTotalRead = m_nTotal; // m_curInventoryBuffer.dtTagDetailTable.Rows.Count;
                        var ts = m_curInventoryBuffer.dtEndInventory - m_curInventoryBuffer.dtStartInventory;
                        var nTotalTime = ts.Minutes * 60 * 1000 + ts.Seconds * 1000 + ts.Milliseconds;
                        var nCaculatedReadRate = 0;
                        var nCommandDuation = 0;

                        if (m_curInventoryBuffer.nReadRate == 0
                        ) //Software measure the speed before reader return speed.
                        {
                            if (nTotalTime > 0) nCaculatedReadRate = nTotalRead * 1000 / nTotalTime;
                        }
                        else
                        {
                            nCommandDuation = m_curInventoryBuffer.nDataCount * 1000 / m_curInventoryBuffer.nReadRate;
                            nCaculatedReadRate = m_curInventoryBuffer.nReadRate;
                        }

                        //Variable of list
                        var nEpcCount = 0;
                        var nEpcLength = m_curInventoryBuffer.dtTagTable.Rows.Count;

                        ledReal1.Text = nTagCount.ToString();
                        ledReal2.Text = nCaculatedReadRate.ToString();

                        ledReal5.Text = nTotalTime.ToString();
                        ledReal3.Text = nTotalRead.ToString();
                        ledReal4.Text = nCommandDuation.ToString(); //The actual command execution time.
                        tbRealMaxRssi.Text = m_curInventoryBuffer.nMaxRSSI - 129 + "dBm";
                        tbRealMinRssi.Text = m_curInventoryBuffer.nMinRSSI - 129 + "dBm";
                        lbRealTagCount.Text = "Tags' EPC list (no-repeat): " + nTagCount + " ";

                        nEpcCount = lvRealList.Items.Count;


                        if (nEpcCount < nEpcLength)
                        {
                            var row = m_curInventoryBuffer.dtTagTable.Rows[nEpcLength - 1];

                            var item = new ListViewItem();

                            var id = (nEpcCount + 1).ToString();
                            var epc = row[2].ToString().Replace(" ", "");
                            var pc = row[0].ToString();
                            var idCount = row[5].ToString();
                            var rssi = Convert.ToInt32(row[4]) - 129 + "dBm";
                            var freq = row[6].ToString();

                            item.Text = id;
                            item.SubItems.Add(epc);
                            item.SubItems.Add(pc);
                            item.SubItems.Add(idCount);
                            item.SubItems.Add(rssi);
                            item.SubItems.Add(freq);
                            lvRealList.Items.Add(item);
                            lvRealList.Items[nEpcCount].EnsureVisible();
                            if (cbWriteDB.Checked) WriteToDatabase(epc);
                        }
                        else
                        {
                            var nIndex = 0;
                            foreach (DataRow row in m_curInventoryBuffer.dtTagTable.Rows)
                            {
                                var item = lvRealList.Items[nIndex];
                                var epc = row[2].ToString().Replace(" ", "");
                                item.SubItems[3].Text = row[5].ToString();
                                nIndex++;
                                if (cbWriteDB.Checked && int.Parse(row[5].ToString()) <= 50) WriteToDatabase(epc);
                            }
                        }

                        //Update the number of read time in list.
                        if (m_nTotal % m_nRealRate == 1)
                        {
                            var nIndex = 0;
                            foreach (DataRow row in m_curInventoryBuffer.dtTagTable.Rows)
                            {
                                ListViewItem item;
                                item = lvRealList.Items[nIndex];
                                item.SubItems[3].Text = row[5].ToString();
                                item.SubItems[4].Text = Convert.ToInt32(row[4]) - 129 + "dBm";
                                item.SubItems[5].Text = row[6].ToString();

                                nIndex++;
                            }
                        }

                        //if (ltvInventoryEpc.SelectedIndices.Count != 0)
                        //{
                        //    int nDetailCount = ltvInventoryTag.Items.Count;
                        //    int nDetailLength = m_curInventoryBuffer.dtTagDetailTable.Rows.Count;

                        //    foreach (int nIndex in ltvInventoryEpc.SelectedIndices)
                        //    {
                        //        ListViewItem itemEpc = ltvInventoryEpc.Items[nIndex];
                        //        DataRow row = m_curInventoryBuffer.dtTagDetailTable.Rows[nDetailLength - 1];
                        //        if (itemEpc.SubItems[1].Text == row[0].ToString())
                        //        {
                        //            ListViewItem item = new ListViewItem();
                        //            item.Text = (nDetailCount + 1).ToString();
                        //            item.SubItems.Add(row[0].ToString());

                        //            string strTemp = (Convert.ToInt32(row[1].ToString()) - 129).ToString() + "dBm";
                        //            item.SubItems.Add(strTemp);
                        //            byte byTemp = Convert.ToByte(row[1]);
                        //            if (byTemp > 0x50)
                        //            {
                        //                item.BackColor = Color.PowderBlue;
                        //            }
                        //            else if (byTemp < 0x30)
                        //            {
                        //                item.BackColor = Color.LemonChiffon;
                        //            }

                        //            item.SubItems.Add(row[2].ToString());
                        //            item.SubItems.Add(row[3].ToString());

                        //            ltvInventoryTag.Items.Add(item);
                        //            ltvInventoryTag.Items[nDetailCount].EnsureVisible();
                        //        }
                        //    }
                        //}
                        //else
                        //{
                        //    int nDetailCount = ltvInventoryTag.Items.Count;
                        //    int nDetailLength = m_curInventoryBuffer.dtTagDetailTable.Rows.Count;

                        //    DataRow row = m_curInventoryBuffer.dtTagDetailTable.Rows[nDetailLength - 1];
                        //    ListViewItem item = new ListViewItem();
                        //    item.Text = (nDetailCount + 1).ToString();
                        //    item.SubItems.Add(row[0].ToString());

                        //    string strTemp = (Convert.ToInt32(row[1].ToString()) - 129).ToString() + "dBm";
                        //    item.SubItems.Add(strTemp);
                        //    byte byTemp = Convert.ToByte(row[1]);
                        //    if (byTemp > 0x50)
                        //    {
                        //        item.BackColor = Color.PowderBlue;
                        //    }
                        //    else if (byTemp < 0x30)
                        //    {
                        //        item.BackColor = Color.LemonChiffon;
                        //    }

                        //    item.SubItems.Add(row[2].ToString());
                        //    item.SubItems.Add(row[3].ToString());

                        //    ltvInventoryTag.Items.Add(item);
                        //    ltvInventoryTag.Items[nDetailCount].EnsureVisible();
                        //}
                    }
                        break;


                    case 0x00:
                    case 0x01:
                    {
                        m_bLockTab = false;
                    }
                        break;
                }
            }
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private void WriteToDatabase(string epc)
        {
            var dbCon = DBConnection.Instance();
            dbCon.DatabaseName = "myrace";
            if (dbCon.IsConnect())
            {
                var query =
                    "INSERT INTO reader (`edition_ID`, `gateway`, `rfid_adress`, `epc`,  `year`, `time`, `created_at`, `updated_at`)" +
                    "VALUES(@id, @gateway, @rfid, @epc, @year, @time, @created, @updated)"; //"SELECT * FROM admin WHERE admin_username=@val1 AND admin_password=PASSWORD(@val2)"
                var cmd = new MySqlCommand(query, dbCon.Connection);
                var currentTime = DateTime.Now.ToLocalTime();
                cmd.Parameters.AddWithValue("@id", (int) numericUpDown1.Value);
                cmd.Parameters.AddWithValue("@gateway", 'F');
                cmd.Parameters.AddWithValue("@rfid", GetLocalIPAddress());
                cmd.Parameters.AddWithValue("@epc", epc);
                cmd.Parameters.AddWithValue("@year", DateTime.Now.Year);
                cmd.Parameters.AddWithValue("@time", currentTime);
                cmd.Parameters.AddWithValue("@created", currentTime);
                cmd.Parameters.AddWithValue("@updated", currentTime);

                //DateTime.Now.Millisecond;

                cmd.Prepare();
                Console.WriteLine($"Result is {cmd.ExecuteNonQuery()}");
                /*while (executeReader.Read())
                {
                    var someStringFromColumnZero = executeReader.GetString(0);
                    var someStringFromColumnOne = executeReader.GetString(4);
                    //Console.WriteLine($"{someStringFromColumnZero},{someStringFromColumnOne}");
                }*/
                //dbCon.Close();
            }
        }

        private void RefreshFastSwitch(byte btCmd)
        {
            if (InvokeRequired)
            {
                var InvokeRefreshFastSwitch = new RefreshFastSwitchUnsafe(RefreshFastSwitch);
                Invoke(InvokeRefreshFastSwitch, btCmd);
            }
            else
            {
                switch (btCmd)
                {
                    case 0x00:
                    {
                        var nTagCount = m_curInventoryBuffer.dtTagTable.Rows.Count;
                        var nTotalRead = m_nTotal; // m_curInventoryBuffer.dtTagDetailTable.Rows.Count;
                        var ts = m_curInventoryBuffer.dtEndInventory - m_curInventoryBuffer.dtStartInventory;
                        var nTotalTime = ts.Minutes * 60 * 1000 + ts.Seconds * 1000 + ts.Milliseconds;

                        ledFast1.Text = nTagCount.ToString(); //Total number of tags
                        if (m_curInventoryBuffer.nCommandDuration > 0)
                            ledFast2.Text =
                                (m_curInventoryBuffer.nDataCount * 1000 / m_curInventoryBuffer.nCommandDuration)
                                .ToString(); //Read speed
                        else
                            ledFast2.Text = "";

                        ledFast3.Text = m_curInventoryBuffer.nCommandDuration.ToString(); //Command duration

                        ledFast5.Text = nTotalTime.ToString(); //Total inventory duration
                        ledFast4.Text = nTotalRead.ToString();

                        txtFastMaxRssi.Text = m_curInventoryBuffer.nMaxRSSI - 129 + "dBm";
                        txtFastMinRssi.Text = m_curInventoryBuffer.nMinRSSI - 129 + "dBm";
                        txtFastTagList.Text = "Tags' EPC list (no-repeat): " + nTagCount + " ";

                        //Forming the list
                        var nEpcCount = lvFastList.Items.Count;
                        var nEpcLength = m_curInventoryBuffer.dtTagTable.Rows.Count;
                        if (nEpcCount < nEpcLength)
                        {
                            var row = m_curInventoryBuffer.dtTagTable.Rows[nEpcLength - 1];

                            var item = new ListViewItem();
                            item.Text = (nEpcCount + 1).ToString();
                            item.SubItems.Add(row[2].ToString());
                            item.SubItems.Add(row[0].ToString());
                            //item.SubItems.Add(row[5].ToString());
                            item.SubItems.Add(row[7] + "  /  " + row[8] + "  /  " + row[9] + "  /  " + row[10]);
                            item.SubItems.Add(Convert.ToInt32(row[4]) - 129 + "dBm");
                            item.SubItems.Add(row[6].ToString());

                            lvFastList.Items.Add(item);
                            lvFastList.Items[nEpcCount].EnsureVisible();
                        }

                        //Update read frequency of list
                        if (m_nTotal % m_nRealRate == 1)
                        {
                            var nIndex = 0;
                            foreach (DataRow row in m_curInventoryBuffer.dtTagTable.Rows)
                            {
                                var item = lvFastList.Items[nIndex];
                                //item.SubItems[3].Text = row[5].ToString();
                                item.SubItems[3].Text =
                                    row[7] + "  /  " + row[8] + "  /  " + row[9] + "  /  " + row[10];
                                item.SubItems[4].Text = Convert.ToInt32(row[4]) - 129 + "dBm";
                                item.SubItems[5].Text = row[6].ToString();

                                nIndex++;
                            }
                        }
                    }
                        break;
                    case 0x01:
                    {
                    }
                        break;
                    case 0x02:
                    {
                        //ledFast1.Text.Text = m_nSwitchTime.ToString();
                        //ledFast1.Text.Text = m_nSwitchTotal.ToString();
                    }
                        break;
                }
            }
        }

        private void RefreshReadSetting(byte btCmd)
        {
            if (InvokeRequired)
            {
                var InvokeRefresh = new RefreshReadSettingUnsafe(RefreshReadSetting);
                Invoke(InvokeRefresh, btCmd);
            }
            else
            {
                htxtReadId.Text = string.Format("{0:X2}", m_curSetting.btReadId);
                switch (btCmd)
                {
                    case 0x6A:
                        if (m_curSetting.btLinkProfile == 0xd0)
                            rdbProfile0.Checked = true;
                        else if (m_curSetting.btLinkProfile == 0xd1)
                            rdbProfile1.Checked = true;
                        else if (m_curSetting.btLinkProfile == 0xd2)
                            rdbProfile2.Checked = true;
                        else if (m_curSetting.btLinkProfile == 0xd3) rdbProfile3.Checked = true;

                        break;
                    case 0x68:
                        htbGetIdentifier.Text = m_curSetting.btReaderIdentifier;

                        break;
                    case 0x72:
                    {
                        txtFirmwareVersion.Text = m_curSetting.btMajor + "." + m_curSetting.btMinor;
                    }
                        break;
                    case 0x75:
                    {
                        cmbWorkAnt.SelectedIndex = m_curSetting.btWorkAntenna;
                    }
                        break;
                    case 0x77:
                    {
                        txtOutputPower.Text = m_curSetting.btOutputPower.ToString();
                    }
                        break;
                    case 0x79:
                    {
                        switch (m_curSetting.btRegion)
                        {
                            case 0x01:
                            {
                                cbUserDefineFreq.Checked = false;
                                textStartFreq.Text = "";
                                TextFreqInterval.Text = "";
                                textFreqQuantity.Text = "";
                                rdbRegionFcc.Checked = true;
                                cmbFrequencyStart.SelectedIndex = Convert.ToInt32(m_curSetting.btFrequencyStart) - 7;
                                cmbFrequencyEnd.SelectedIndex = Convert.ToInt32(m_curSetting.btFrequencyEnd) - 7;
                            }
                                break;
                            case 0x02:
                            {
                                cbUserDefineFreq.Checked = false;
                                textStartFreq.Text = "";
                                TextFreqInterval.Text = "";
                                textFreqQuantity.Text = "";
                                rdbRegionEtsi.Checked = true;
                                cmbFrequencyStart.SelectedIndex = Convert.ToInt32(m_curSetting.btFrequencyStart);
                                cmbFrequencyEnd.SelectedIndex = Convert.ToInt32(m_curSetting.btFrequencyEnd);
                            }
                                break;
                            case 0x03:
                            {
                                cbUserDefineFreq.Checked = false;
                                textStartFreq.Text = "";
                                TextFreqInterval.Text = "";
                                textFreqQuantity.Text = "";
                                rdbRegionChn.Checked = true;
                                cmbFrequencyStart.SelectedIndex = Convert.ToInt32(m_curSetting.btFrequencyStart) - 43;
                                cmbFrequencyEnd.SelectedIndex = Convert.ToInt32(m_curSetting.btFrequencyEnd) - 43;
                            }
                                break;
                            case 0x04:
                            {
                                cbUserDefineFreq.Checked = true;
                                rdbRegionChn.Checked = false;
                                rdbRegionEtsi.Checked = false;
                                rdbRegionFcc.Checked = false;
                                cmbFrequencyStart.SelectedIndex = -1;
                                cmbFrequencyEnd.SelectedIndex = -1;
                                textStartFreq.Text = m_curSetting.nUserDefineStartFrequency.ToString();
                                TextFreqInterval.Text =
                                    Convert.ToString(m_curSetting.btUserDefineFrequencyInterval * 10);
                                textFreqQuantity.Text = m_curSetting.btUserDefineChannelQuantity.ToString();
                            }
                                break;
                        }
                    }
                        break;
                    case 0x7B:
                    {
                        var strTemperature = string.Empty;
                        if (m_curSetting.btPlusMinus == 0x0)
                            strTemperature = "-" + m_curSetting.btTemperature + "℃";
                        else
                            strTemperature = m_curSetting.btTemperature + "℃";
                        txtReaderTemperature.Text = strTemperature;
                    }
                        break;
                    case 0x7D:
                    {
                        if (m_curSetting.btDrmMode == 0x00)
                            rdbDrmModeClose.Checked = true;
                        else
                            rdbDrmModeOpen.Checked = true;
                    }
                        break;
                    case 0x7E:
                    {
                        textReturnLoss.Text = m_curSetting.btAntImpedance + " dB";
                    }
                        break;


                    case 0x8E:
                    {
                        if (m_curSetting.btMonzaStatus == 0x8D)
                            rdbMonzaOn.Checked = true;
                        else
                            rdbMonzaOff.Checked = true;
                    }
                        break;
                    case 0x60:
                    {
                        if (m_curSetting.btGpio1Value == 0x00)
                            rdbGpio1Low.Checked = true;
                        else
                            rdbGpio1High.Checked = true;

                        if (m_curSetting.btGpio2Value == 0x00)
                            rdbGpio2Low.Checked = true;
                        else
                            rdbGpio2High.Checked = true;
                    }
                        break;
                    case 0x63:
                    {
                        tbAntDectector.Text = m_curSetting.btAntDetector.ToString();
                    }
                        break;
                }
            }
        }

        private void RunLoopInventroy()
        {
            if (InvokeRequired)
            {
                var InvokeRunLoopInventory = new RunLoopInventoryUnsafe(RunLoopInventroy);
                Invoke(InvokeRunLoopInventory, new object[] { });
            }
            else
            {
                //Verify whether all antennas are completed inventory
                if (m_curInventoryBuffer.nIndexAntenna < m_curInventoryBuffer.lAntenna.Count - 1 ||
                    m_curInventoryBuffer.nCommond == 0)
                {
                    if (m_curInventoryBuffer.nCommond == 0)
                    {
                        m_curInventoryBuffer.nCommond = 1;

                        if (m_curInventoryBuffer.bLoopInventoryReal)
                        {
                            //m_bLockTab = true;
                            //btnInventory.Enabled = false;
                            if (m_curInventoryBuffer.bLoopCustomizedSession
                            ) //User define Session and Inventoried Flag. 
                                reader.CustomizedInventory(m_curSetting.btReadId, m_curInventoryBuffer.btSession,
                                    m_curInventoryBuffer.btTarget, m_curInventoryBuffer.btRepeat);
                            else //Inventory tags in real time mode
                                reader.InventoryReal(m_curSetting.btReadId, m_curInventoryBuffer.btRepeat);
                        }
                        else
                        {
                            if (m_curInventoryBuffer.bLoopInventory)
                                reader.Inventory(m_curSetting.btReadId, m_curInventoryBuffer.btRepeat);
                        }
                    }
                    else
                    {
                        m_curInventoryBuffer.nCommond = 0;
                        m_curInventoryBuffer.nIndexAntenna++;

                        var btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                        reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                        m_curSetting.btWorkAntenna = btWorkAntenna;
                    }
                }
                //Verify whether cycle inventory
                else if (m_curInventoryBuffer.bLoopInventory)
                {
                    m_curInventoryBuffer.nIndexAntenna = 0;
                    m_curInventoryBuffer.nCommond = 0;

                    var btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                    reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                    m_curSetting.btWorkAntenna = btWorkAntenna;
                }
            }
        }

        private void RunLoopFastSwitch()
        {
            if (InvokeRequired)
            {
                var InvokeRunLoopFastSwitch = new RunLoopFastSwitchUnsafe(RunLoopFastSwitch);
                Invoke(InvokeRunLoopFastSwitch, new object[] { });
            }
            else
            {
                if (m_curInventoryBuffer.bLoopInventory) reader.FastSwitchInventory(m_curSetting.btReadId, m_btAryData);
            }
        }

        private void RefreshISO18000(byte btCmd)
        {
            if (InvokeRequired)
            {
                var InvokeRefreshISO18000 = new RefreshISO18000Unsafe(RefreshISO18000);
                Invoke(InvokeRefreshISO18000, btCmd);
            }
            else
            {
                switch (btCmd)
                {
                    case 0xb0:
                    {
                        ltvTagISO18000.Items.Clear();
                        var nLength = m_curOperateTagISO18000Buffer.dtTagTable.Rows.Count;
                        var nIndex = 1;
                        foreach (DataRow row in m_curOperateTagISO18000Buffer.dtTagTable.Rows)
                        {
                            var item = new ListViewItem();
                            item.Text = nIndex.ToString();
                            item.SubItems.Add(row[1].ToString());
                            item.SubItems.Add(row[0].ToString());
                            item.SubItems.Add(row[2].ToString());
                            ltvTagISO18000.Items.Add(item);

                            nIndex++;
                        }

                        //txtTagCountISO18000.Text = m_curOperateTagISO18000Buffer.dtTagTable.Rows.Count.ToString();

                        if (m_bContinue)
                            reader.InventoryISO18000(m_curSetting.btReadId);
                        else
                            WriteLog(lrtxtLog, "Stop", 0);
                    }
                        break;
                    case 0xb1:
                    {
                        htxtReadData18000.Text = m_curOperateTagISO18000Buffer.strReadData;
                    }
                        break;
                    case 0xb2:
                    {
                        //txtWriteLength.Text = m_curOperateTagISO18000Buffer.btWriteLength.ToString();
                    }
                        break;
                    case 0xb3:
                    {
                        //switch(m_curOperateTagISO18000Buffer.btStatus)
                        //{
                        //    case 0x00:
                        //        MessageBox.Show("The byte successfully locked");
                        //        break;
                        //    case 0xFE:
                        //        MessageBox.Show("Status of the byte is locked");
                        //        break;
                        //    case 0xFF:
                        //        MessageBox.Show("The byte can not be locked");
                        //        break;
                        //    default:
                        //        break;
                        //}
                    }
                        break;
                    case 0xb4:
                    {
                        switch (m_curOperateTagISO18000Buffer.btStatus)
                        {
                            case 0x00:
                                txtStatus.Text = "This byte is not locked";
                                break;
                            case 0xFE:
                                txtStatus.Text = "Status of the byte is locked";
                                break;
                        }
                    }
                        break;
                }
            }
        }

        private void RunLoopISO18000(int nLength)
        {
            if (InvokeRequired)
            {
                var InvokeRunLoopISO18000 = new RunLoopISO18000Unsafe(RunLoopISO18000);
                Invoke(InvokeRunLoopISO18000, nLength);
            }
            else
            {
                //Judge whether write correctly.
                if (nLength == m_nBytes)
                {
                    m_nLoopedTimes++;
                    txtLoopTimes.Text = m_nLoopedTimes.ToString();
                }

                //Judge whether cycle is ended.
                m_nLoopTimes--;
                if (m_nLoopTimes > 0) WriteTagISO18000();
            }
        }

        private void rdbRS232_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbRS232.Checked)
            {
                gbRS232.Enabled = true;
                btnDisconnectRs232.Enabled = false;

                //Set button font color
                btnConnectRs232.ForeColor = Color.Indigo;
                SetButtonBold(btnConnectRs232);
                if (btnConnectTcp.Font.Bold) SetButtonBold(btnConnectTcp);

                gbTcpIp.Enabled = false;
            }
        }

        private void rdbTcpIp_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbTcpIp.Checked)
            {
                gbTcpIp.Enabled = true;
                btnDisconnectTcp.Enabled = false;

                //Set button font color
                btnConnectTcp.ForeColor = Color.Indigo;
                if (btnConnectRs232.Font.Bold) SetButtonBold(btnConnectRs232);
                SetButtonBold(btnConnectTcp);

                gbRS232.Enabled = false;
            }
        }

        private void SetButtonBold(Button btnBold)
        {
            var oldFont = btnBold.Font;
            var newFont = new Font(oldFont, oldFont.Style ^ FontStyle.Bold);
            btnBold.Font = newFont;
        }

        private void SetRadioButtonBold(CheckBox ckBold)
        {
            var oldFont = ckBold.Font;
            var newFont = new Font(oldFont, oldFont.Style ^ FontStyle.Bold);
            ckBold.Font = newFont;
        }

        private void SetFormEnable(bool bIsEnable)
        {
            gbConnectType.Enabled = !bIsEnable;
            gbCmdReaderAddress.Enabled = bIsEnable;
            gbCmdVersion.Enabled = bIsEnable;
            gbCmdBaudrate.Enabled = bIsEnable;
            gbCmdTemperature.Enabled = bIsEnable;
            gbCmdOutputPower.Enabled = bIsEnable;
            gbCmdAntenna.Enabled = bIsEnable;
            gbCmdDrm.Enabled = bIsEnable;
            gbCmdRegion.Enabled = bIsEnable;
            gbCmdBeeper.Enabled = bIsEnable;
            gbCmdReadGpio.Enabled = bIsEnable;
            gbCmdAntDetector.Enabled = bIsEnable;
            gbReturnLoss.Enabled = bIsEnable;
            gbProfile.Enabled = bIsEnable;

            btnResetReader.Enabled = bIsEnable;


            gbCmdOperateTag.Enabled = bIsEnable;

            btnInventoryISO18000.Enabled = bIsEnable;
            btnClear.Enabled = bIsEnable;
            gbISO1800ReadWrite.Enabled = bIsEnable;
            gbISO1800LockQuery.Enabled = bIsEnable;

            gbCmdManual.Enabled = bIsEnable;

            tabEpcTest.Enabled = bIsEnable;

            gbMonza.Enabled = bIsEnable;
            lbChangeBaudrate.Enabled = bIsEnable;
            cmbSetBaudrate.Enabled = bIsEnable;
            btnSetUartBaudrate.Enabled = bIsEnable;
            btReaderSetupRefresh.Enabled = bIsEnable;

            btRfSetup.Enabled = bIsEnable;
        }

        private void btnConnectRs232_Click(object sender, EventArgs e)
        {
            //Processing serial port to connect reader.
            var strException = string.Empty;
            var strComPort = cmbComPort.Text;
            var nBaudrate = Convert.ToInt32(cmbBaudrate.Text);

            var nRet = reader.OpenCom(strComPort, nBaudrate, out strException);
            if (nRet != 0)
            {
                var strLog = "Connection failed, failure cause: " + strException;
                WriteLog(lrtxtLog, strLog, 1);

                return;
            }
            else
            {
                var strLog = "Connect" + strComPort + "@" + nBaudrate;
                WriteLog(lrtxtLog, strLog, 0);
            }

            //Whether processing interface element is valid.
            SetFormEnable(true);


            btnConnectRs232.Enabled = false;
            btnDisconnectRs232.Enabled = true;

            //Set button font color.
            btnConnectRs232.ForeColor = Color.Black;
            btnDisconnectRs232.ForeColor = Color.Indigo;
            SetButtonBold(btnConnectRs232);
            SetButtonBold(btnDisconnectRs232);
        }

        private void btnDisconnectRs232_Click(object sender, EventArgs e)
        {
            //Processing serial port to disconnect reader.
            reader.CloseCom();

            //Whether processing interface element is valid.
            SetFormEnable(false);
            btnConnectRs232.Enabled = true;
            btnDisconnectRs232.Enabled = false;

            //Set button font color.
            btnConnectRs232.ForeColor = Color.Indigo;
            btnDisconnectRs232.ForeColor = Color.Black;
            SetButtonBold(btnConnectRs232);
            SetButtonBold(btnDisconnectRs232);
        }

        private void btnConnectTcp_Click(object sender, EventArgs e)
        {
            try
            {
                //Processing Tcp to connect reader.
                var strException = string.Empty;
                var ipAddress = IPAddress.Parse(ipIpServer.IpAddressStr);
                var nPort = Convert.ToInt32(txtTcpPort.Text);

                var nRet = reader.ConnectServer(ipAddress, nPort, out strException);
                if (nRet != 0)
                {
                    var strLog = "Connection failed, failure cause: " + strException;
                    WriteLog(lrtxtLog, strLog, 1);

                    return;
                }
                else
                {
                    var strLog = "Connect" + ipIpServer.IpAddressStr + "@" + nPort;
                    WriteLog(lrtxtLog, strLog, 0);
                }

                //Whether processing interface element is valid.
                SetFormEnable(true);
                btnConnectTcp.Enabled = false;
                btnDisconnectTcp.Enabled = true;

                //Set button font color.
                btnConnectTcp.ForeColor = Color.Black;
                btnDisconnectTcp.ForeColor = Color.Indigo;
                SetButtonBold(btnConnectTcp);
                SetButtonBold(btnDisconnectTcp);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnDisconnectTcp_Click(object sender, EventArgs e)
        {
            //Processing Tcp to disconnect reader.
            reader.SignOut();

            //Whether processing interface element is valid.
            SetFormEnable(false);
            btnConnectTcp.Enabled = true;
            btnDisconnectTcp.Enabled = false;

            //Set button font color.
            btnConnectTcp.ForeColor = Color.Indigo;
            btnDisconnectTcp.ForeColor = Color.Black;
            SetButtonBold(btnConnectTcp);
            SetButtonBold(btnDisconnectTcp);
        }

        private void btnResetReader_Click(object sender, EventArgs e)
        {
            var nRet = reader.Reset(m_curSetting.btReadId);
            if (nRet != 0)
            {
                var strLog = "Reset reader fails";
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                var strLog = "Reset reader";
                WriteLog(lrtxtLog, strLog, 0);
            }
        }

        private void btnSetReadAddress_Click(object sender, EventArgs e)
        {
            try
            {
                if (htxtReadId.Text.Length != 0)
                {
                    var strTemp = htxtReadId.Text.Trim();
                    reader.SetReaderAddress(m_curSetting.btReadId, Convert.ToByte(strTemp, 16));
                    m_curSetting.btReadId = Convert.ToByte(strTemp, 16);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ProcessSetReadAddress(MessageTran msgTran)
        {
            var strCmd = "Set reader's address";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnGetFirmwareVersion_Click(object sender, EventArgs e)
        {
            reader.GetFirmwareVersion(m_curSetting.btReadId);
        }

        private void ProcessGetFirmwareVersion(MessageTran msgTran)
        {
            var strCmd = "Get Reader's firmware version";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 2)
            {
                m_curSetting.btMajor = msgTran.AryData[0];
                m_curSetting.btMinor = msgTran.AryData[1];
                m_curSetting.btReadId = msgTran.ReadId;

                RefreshReadSetting(msgTran.Cmd);
                WriteLog(lrtxtLog, strCmd, 0);
                return;
            }

            if (msgTran.AryData.Length == 1)
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            else
                strErrorCode = "Unknown Error";

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnSetUartBaudrate_Click(object sender, EventArgs e)
        {
            if (cmbSetBaudrate.SelectedIndex != -1)
            {
                reader.SetUartBaudrate(m_curSetting.btReadId, cmbSetBaudrate.SelectedIndex + 3);
                m_curSetting.btIndexBaudrate = Convert.ToByte(cmbSetBaudrate.SelectedIndex);
            }
        }

        private void ProcessSetUartBaudrate(MessageTran msgTran)
        {
            var strCmd = "Set Baudrate";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnGetReaderTemperature_Click(object sender, EventArgs e)
        {
            reader.GetReaderTemperature(m_curSetting.btReadId);
        }

        private void ProcessGetReaderTemperature(MessageTran msgTran)
        {
            var strCmd = "Get reader internal temperature";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 2)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btPlusMinus = msgTran.AryData[0];
                m_curSetting.btTemperature = msgTran.AryData[1];

                RefreshReadSetting(msgTran.Cmd);
                WriteLog(lrtxtLog, strCmd, 0);
                return;
            }

            if (msgTran.AryData.Length == 1)
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            else
                strErrorCode = "Unknown Error";

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnGetOutputPower_Click(object sender, EventArgs e)
        {
            reader.GetOutputPower(m_curSetting.btReadId);
        }

        private void ProcessGetOutputPower(MessageTran msgTran)
        {
            var strCmd = "Get RF Output Power";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btOutputPower = msgTran.AryData[0];

                RefreshReadSetting(0x77);
                WriteLog(lrtxtLog, strCmd, 0);
                return;
            }

            strErrorCode = "Unknown Error";

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnSetOutputPower_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtOutputPower.Text.Length != 0)
                {
                    reader.SetOutputPower(m_curSetting.btReadId, Convert.ToByte(txtOutputPower.Text));
                    m_curSetting.btOutputPower = Convert.ToByte(txtOutputPower.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ProcessSetOutputPower(MessageTran msgTran)
        {
            var strCmd = "Set RF Output Power";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnGetWorkAntenna_Click(object sender, EventArgs e)
        {
            reader.GetWorkAntenna(m_curSetting.btReadId);
        }

        private void ProcessGetWorkAntenna(MessageTran msgTran)
        {
            var strCmd = "Get working antenna";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x00 || msgTran.AryData[0] == 0x01 || msgTran.AryData[0] == 0x02 ||
                    msgTran.AryData[0] == 0x03)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    m_curSetting.btWorkAntenna = msgTran.AryData[0];

                    RefreshReadSetting(0x75);
                    WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnSetWorkAntenna_Click(object sender, EventArgs e)
        {
            m_bInventory = false;
            byte btWorkAntenna = 0xFF;
            if (cmbWorkAnt.SelectedIndex != -1)
            {
                btWorkAntenna = (byte) cmbWorkAnt.SelectedIndex;
                reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                m_curSetting.btWorkAntenna = btWorkAntenna;
            }
        }

        private void ProcessSetWorkAntenna(MessageTran msgTran)
        {
            var intCurrentAnt = 0;
            intCurrentAnt = m_curSetting.btWorkAntenna + 1;
            var strCmd = "Set working antenna successfully, Current Ant: Ant" + intCurrentAnt;

            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    WriteLog(lrtxtLog, strCmd, 0);

                    //Verify inventory operations
                    if (m_bInventory) RunLoopInventroy();
                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);

            if (m_bInventory)
            {
                m_curInventoryBuffer.nCommond = 1;
                m_curInventoryBuffer.dtEndInventory = DateTime.Now;
                RunLoopInventroy();
            }
        }

        private void btnGetDrmMode_Click(object sender, EventArgs e)
        {
            reader.GetDrmMode(m_curSetting.btReadId);
        }

        private void ProcessGetDrmMode(MessageTran msgTran)
        {
            var strCmd = "Get DRM Status";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x00 || msgTran.AryData[0] == 0x01)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    m_curSetting.btDrmMode = msgTran.AryData[0];

                    RefreshReadSetting(0x7D);
                    WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnSetDrmMode_Click(object sender, EventArgs e)
        {
            byte btDrmMode = 0xFF;

            if (rdbDrmModeClose.Checked)
                btDrmMode = 0x00;
            else if (rdbDrmModeOpen.Checked)
                btDrmMode = 0x01;
            else
                return;

            reader.SetDrmMode(m_curSetting.btReadId, btDrmMode);
            m_curSetting.btDrmMode = btDrmMode;
        }

        private void ProcessSetDrmMode(MessageTran msgTran)
        {
            var strCmd = "Set DRM Status";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void rdbRegionFcc_CheckedChanged(object sender, EventArgs e)
        {
            cmbFrequencyStart.SelectedIndex = -1;
            cmbFrequencyEnd.SelectedIndex = -1;
            cmbFrequencyStart.Items.Clear();
            cmbFrequencyEnd.Items.Clear();

            var nStart = 902.00f;
            for (var nloop = 0; nloop < 53; nloop++)
            {
                var strTemp = nStart.ToString("0.00");
                cmbFrequencyStart.Items.Add(strTemp);
                cmbFrequencyEnd.Items.Add(strTemp);

                nStart += 0.5f;
            }
        }

        private void rdbRegionEtsi_CheckedChanged(object sender, EventArgs e)
        {
            cmbFrequencyStart.SelectedIndex = -1;
            cmbFrequencyEnd.SelectedIndex = -1;
            cmbFrequencyStart.Items.Clear();
            cmbFrequencyEnd.Items.Clear();

            var nStart = 865.00f;
            for (var nloop = 0; nloop < 7; nloop++)
            {
                var strTemp = nStart.ToString("0.00");
                cmbFrequencyStart.Items.Add(strTemp);
                cmbFrequencyEnd.Items.Add(strTemp);

                nStart += 0.5f;
            }
        }

        private void rdbRegionChn_CheckedChanged(object sender, EventArgs e)
        {
            cmbFrequencyStart.SelectedIndex = -1;
            cmbFrequencyEnd.SelectedIndex = -1;
            cmbFrequencyStart.Items.Clear();
            cmbFrequencyEnd.Items.Clear();

            var nStart = 920.00f;
            for (var nloop = 0; nloop < 11; nloop++)
            {
                var strTemp = nStart.ToString("0.00");
                cmbFrequencyStart.Items.Add(strTemp);
                cmbFrequencyEnd.Items.Add(strTemp);

                nStart += 0.5f;
            }
        }

        private string GetFreqString(byte btFreq)
        {
            var strFreq = string.Empty;

            if (m_curSetting.btRegion == 4)
            {
                float nExtraFrequency = btFreq * m_curSetting.btUserDefineFrequencyInterval * 10;
                var nstartFrequency = (float) m_curSetting.nUserDefineStartFrequency / 1000;
                var nStart = nstartFrequency + nExtraFrequency / 1000;
                var strTemp = nStart.ToString("0.000");
                return strTemp;
            }

            if (btFreq < 0x07)
            {
                var nStart = 865.00f + Convert.ToInt32(btFreq) * 0.5f;

                var strTemp = nStart.ToString("0.00");

                return strTemp;
            }
            else
            {
                var nStart = 902.00f + (Convert.ToInt32(btFreq) - 7) * 0.5f;

                var strTemp = nStart.ToString("0.00");

                return strTemp;
            }
        }

        private void btnGetFrequencyRegion_Click(object sender, EventArgs e)
        {
            reader.GetFrequencyRegion(m_curSetting.btReadId);
        }

        private void ProcessGetFrequencyRegion(MessageTran msgTran)
        {
            var strCmd = "Query RF frequency spectrum";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 3)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btRegion = msgTran.AryData[0];
                m_curSetting.btFrequencyStart = msgTran.AryData[1];
                m_curSetting.btFrequencyEnd = msgTran.AryData[2];

                RefreshReadSetting(0x79);
                WriteLog(lrtxtLog, strCmd, 0);
                return;
            }

            if (msgTran.AryData.Length == 6)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btRegion = msgTran.AryData[0];
                m_curSetting.btUserDefineFrequencyInterval = msgTran.AryData[1];
                m_curSetting.btUserDefineChannelQuantity = msgTran.AryData[2];
                m_curSetting.nUserDefineStartFrequency =
                    msgTran.AryData[3] * 256 * 256 + msgTran.AryData[4] * 256 + msgTran.AryData[5];
                RefreshReadSetting(0x79);
                WriteLog(lrtxtLog, strCmd, 0);
                return;
            }

            if (msgTran.AryData.Length == 1)
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            else
                strErrorCode = "Unknown Error";

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnSetFrequencyRegion_Click(object sender, EventArgs e)
        {
            try
            {
                if (cbUserDefineFreq.Checked)
                {
                    var nStartFrequency = Convert.ToInt32(textStartFreq.Text);
                    var nFrequencyInterval = Convert.ToInt32(TextFreqInterval.Text);
                    nFrequencyInterval = nFrequencyInterval / 10;
                    var btChannelQuantity = Convert.ToByte(textFreqQuantity.Text);
                    reader.SetUserDefineFrequency(m_curSetting.btReadId, nStartFrequency, (byte) nFrequencyInterval,
                        btChannelQuantity);
                    m_curSetting.btRegion = 4;
                    m_curSetting.nUserDefineStartFrequency = nStartFrequency;
                    m_curSetting.btUserDefineFrequencyInterval = (byte) nFrequencyInterval;
                    m_curSetting.btUserDefineChannelQuantity = btChannelQuantity;
                }
                else
                {
                    byte btRegion = 0x00;
                    byte btStartFreq = 0x00;
                    byte btEndFreq = 0x00;

                    var nStartIndex = cmbFrequencyStart.SelectedIndex;
                    var nEndIndex = cmbFrequencyEnd.SelectedIndex;
                    if (nEndIndex < nStartIndex)
                    {
                        MessageBox.Show(
                            "Spectral range that does not meet specifications, please refer to the Serial Protocol");
                        return;
                    }

                    if (rdbRegionFcc.Checked)
                    {
                        btRegion = 0x01;
                        btStartFreq = Convert.ToByte(nStartIndex + 7);
                        btEndFreq = Convert.ToByte(nEndIndex + 7);
                    }
                    else if (rdbRegionEtsi.Checked)
                    {
                        btRegion = 0x02;
                        btStartFreq = Convert.ToByte(nStartIndex);
                        btEndFreq = Convert.ToByte(nEndIndex);
                    }
                    else if (rdbRegionChn.Checked)
                    {
                        btRegion = 0x03;
                        btStartFreq = Convert.ToByte(nStartIndex + 43);
                        btEndFreq = Convert.ToByte(nEndIndex + 43);
                    }
                    else
                    {
                        return;
                    }

                    reader.SetFrequencyRegion(m_curSetting.btReadId, btRegion, btStartFreq, btEndFreq);
                    m_curSetting.btRegion = btRegion;
                    m_curSetting.btFrequencyStart = btStartFreq;
                    m_curSetting.btFrequencyEnd = btEndFreq;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ProcessSetFrequencyRegion(MessageTran msgTran)
        {
            var strCmd = "Set RF frequency spectrum";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnSetBeeperMode_Click(object sender, EventArgs e)
        {
            byte btBeeperMode = 0xFF;

            if (rdbBeeperModeSlient.Checked)
                btBeeperMode = 0x00;
            else if (rdbBeeperModeInventory.Checked)
                btBeeperMode = 0x01;
            else if (rdbBeeperModeTag.Checked)
                btBeeperMode = 0x02;
            else
                return;

            reader.SetBeeperMode(m_curSetting.btReadId, btBeeperMode);
            m_curSetting.btBeeperMode = btBeeperMode;
        }

        private void ProcessSetBeeperMode(MessageTran msgTran)
        {
            var strCmd = "Set reader's buzzer hehavior";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnReadGpioValue_Click(object sender, EventArgs e)
        {
            reader.ReadGpioValue(m_curSetting.btReadId);
        }

        private void ProcessReadGpioValue(MessageTran msgTran)
        {
            var strCmd = "Get GPIO status";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 2)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btGpio1Value = msgTran.AryData[0];
                m_curSetting.btGpio2Value = msgTran.AryData[1];

                RefreshReadSetting(0x60);
                WriteLog(lrtxtLog, strCmd, 0);
                return;
            }

            if (msgTran.AryData.Length == 1)
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            else
                strErrorCode = "Unknown Error";

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnWriteGpio3Value_Click(object sender, EventArgs e)
        {
            byte btGpioValue = 0xFF;

            if (rdbGpio3Low.Checked)
                btGpioValue = 0x00;
            else if (rdbGpio3High.Checked)
                btGpioValue = 0x01;
            else
                return;

            reader.WriteGpioValue(m_curSetting.btReadId, 0x03, btGpioValue);
            m_curSetting.btGpio3Value = btGpioValue;
        }

        private void btnWriteGpio4Value_Click(object sender, EventArgs e)
        {
            byte btGpioValue = 0xFF;

            if (rdbGpio4Low.Checked)
                btGpioValue = 0x00;
            else if (rdbGpio4High.Checked)
                btGpioValue = 0x01;
            else
                return;

            reader.WriteGpioValue(m_curSetting.btReadId, 0x04, btGpioValue);
            m_curSetting.btGpio4Value = btGpioValue;
        }

        private void ProcessWriteGpioValue(MessageTran msgTran)
        {
            var strCmd = "Set GPIO status";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnGetAntDetector_Click(object sender, EventArgs e)
        {
            reader.GetAntDetector(m_curSetting.btReadId);
        }

        private void ProcessGetAntDetector(MessageTran msgTran)
        {
            var strCmd = "Get antenna detector threshold value";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btAntDetector = msgTran.AryData[0];

                RefreshReadSetting(0x63);
                WriteLog(lrtxtLog, strCmd, 0);
                return;
            }

            strErrorCode = "Unknown Error";

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void ProcessGetMonzaStatus(MessageTran msgTran)
        {
            var strCmd = "Get current Impinj FastTID setting";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x00 || msgTran.AryData[0] == 0x8D)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    m_curSetting.btAntDetector = msgTran.AryData[0];

                    RefreshReadSetting(0x8E);
                    WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void ProcessSetMonzaStatus(MessageTran msgTran)
        {
            var strCmd = "Set Impinj FastTID function";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    m_curSetting.btAntDetector = msgTran.AryData[0];

                    WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void ProcessSetProfile(MessageTran msgTran)
        {
            var strCmd = "Set RF link profile";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    m_curSetting.btLinkProfile = msgTran.AryData[0];

                    WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void ProcessGetProfile(MessageTran msgTran)
        {
            var strCmd = "Get RF link profile";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] >= 0xd0 && msgTran.AryData[0] <= 0xd3)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    m_curSetting.btLinkProfile = msgTran.AryData[0];

                    RefreshReadSetting(0x6A);
                    WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }


        private void ProcessGetReaderIdentifier(MessageTran msgTran)
        {
            var strCmd = "Get Reader Identifier";
            var strErrorCode = string.Empty;
            short i;
            var readerIdentifier = "";

            if (msgTran.AryData.Length == 12)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                for (i = 0; i < 12; i++)
                    readerIdentifier = readerIdentifier + string.Format("{0:X2}", msgTran.AryData[i]) + " ";
                m_curSetting.btReaderIdentifier = readerIdentifier;
                RefreshReadSetting(0x68);

                WriteLog(lrtxtLog, strCmd, 0);
                return;
            }

            strErrorCode = "Unknown Error";

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void ProcessGetImpedanceMatch(MessageTran msgTran)
        {
            var strCmd = "Measure Impedance of Antenna Port Match";
            var strErrorCode = string.Empty;


            if (msgTran.AryData.Length == 1)
            {
                m_curSetting.btReadId = msgTran.ReadId;

                m_curSetting.btAntImpedance = msgTran.AryData[0];
                RefreshReadSetting(0x7E);

                WriteLog(lrtxtLog, strCmd, 0);
                return;
            }

            strErrorCode = "Unknown Error";

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }


        private void ProcessSetReaderIdentifier(MessageTran msgTran)
        {
            var strCmd = "Set Reader Identifier";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }


        private void btnSetAntDetector_Click(object sender, EventArgs e)
        {
            try
            {
                if (tbAntDectector.Text.Length != 0)
                {
                    reader.SetAntDetector(m_curSetting.btReadId, Convert.ToByte(tbAntDectector.Text));
                    m_curSetting.btAntDetector = Convert.ToByte(tbAntDectector.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ProcessSetAntDetector(MessageTran msgTran)
        {
            var strCmd = "Set antenna detector threshold value";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);
        }

        private void rdbInventoryTag_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void rdbOperateTag_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void rdbInventoryRealTag_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void rbdFastSwitchInventory_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void btnInventory_Click(object sender, EventArgs e)
        {
            /*try
            {                
                if (rbdFastSwitchInventory.Checked)
                {
                }
                else
                {
                    m_curInventoryBuffer.ClearInventoryPar();

                    if (txtChannel.Text.Length == 0)
                    {
                        MessageBox.Show("Please enter frequency hopping No.");
                        return;
                    }
                    m_curInventoryBuffer.btChannel = Convert.ToByte(txtChannel.Text);

                    if (ckWorkAntenna1.Checked)
                    {
                        m_curInventoryBuffer.lAntenna.Add(0x00);
                    }
                    if (ckWorkAntenna2.Checked)
                    {
                        m_curInventoryBuffer.lAntenna.Add(0x01);
                    }
                    if (ckWorkAntenna3.Checked)
                    {
                        m_curInventoryBuffer.lAntenna.Add(0x02);
                    }
                    if (ckWorkAntenna4.Checked)
                    {
                        m_curInventoryBuffer.lAntenna.Add(0x03);
                    }
                    if (m_curInventoryBuffer.lAntenna.Count == 0)
                    {
                        MessageBox.Show("One antenna must be selected");
                        return;
                    }
                }                

                //Default cycle to send commands.
                if (m_curInventoryBuffer.bLoopInventory)
                {
                    m_bInventory = false;
                    m_curInventoryBuffer.bLoopInventory = false;
                    btnInventory.BackColor = Color.WhiteSmoke;
                    btnInventory.ForeColor = Color.Indigo;
                    btnInventory.Text = "Inventory";
                    return;
                }
                else
                {
                    //Whether ISO 18000-6B Inventory is runing.
                    if (m_bContinue)
                    {
                        if (MessageBox.Show("ISO 18000-6B tag is inventoring, whether to stop?", "Prompt", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Cancel)
                        {
                            return;
                        }
                        else
                        {
                            btnInventoryISO18000_Click(sender, e);
                            return;
                        }
                    }

                    m_bInventory = true; 
                    m_curInventoryBuffer.bLoopInventory = true;
                    btnInventory.BackColor = Color.Indigo;
                    btnInventory.ForeColor = Color.White;
                    btnInventory.Text = "Stop";
                }

                if (rdbInventoryRealTag.Checked)
                {
                    m_curInventoryBuffer.bLoopInventoryReal = true;
                }

                m_curInventoryBuffer.ClearInventoryRealResult();
                ltvInventoryEpc.Items.Clear();
                ltvInventoryTag.Items.Clear();
                m_nTotal = 0;
                if (rbdFastSwitchInventory.Checked)
                {
                    if (cmbAntSelect1.SelectedIndex == -1)
                    {
                        m_btAryData[0] = 0xFF;
                    }
                    else
                    {
                        m_btAryData[0] = Convert.ToByte(cmbAntSelect1.SelectedIndex);
                    }
                    if (txtStayA.Text.Length == 0)
                    {
                        m_btAryData[1] = 0x00;
                    }
                    else
                    {
                        m_btAryData[1] = Convert.ToByte(txtStayA.Text);
                    }

                    if (cmbAntSelect2.SelectedIndex == -1)
                    {
                        m_btAryData[2] = 0xFF;
                    }
                    else
                    {
                        m_btAryData[2] = Convert.ToByte(cmbAntSelect2.SelectedIndex);
                    }
                    if (txtStayB.Text.Length == 0)
                    {
                        m_btAryData[3] = 0x00;
                    }
                    else
                    {
                        m_btAryData[3] = Convert.ToByte(txtStayB.Text);
                    }

                    if (cmbAntSelect3.SelectedIndex == -1)
                    {
                        m_btAryData[4] = 0xFF;
                    }
                    else
                    {
                        m_btAryData[4] = Convert.ToByte(cmbAntSelect3.SelectedIndex);
                    }
                    if (txtStayC.Text.Length == 0)
                    {
                        m_btAryData[5] = 0x00;
                    }
                    else
                    {
                        m_btAryData[5] = Convert.ToByte(txtStayC.Text);
                    }

                    if (cmbAntSelect4.SelectedIndex == -1)
                    {
                        m_btAryData[6] = 0xFF;
                    }
                    else
                    {
                        m_btAryData[6] = Convert.ToByte(cmbAntSelect4.SelectedIndex);
                    }
                    if (txtStayD.Text.Length == 0)
                    {
                        m_btAryData[7] = 0x00;
                    }
                    else
                    {
                        m_btAryData[7] = Convert.ToByte(txtStayD.Text);
                    }

                    if (txtInterval.Text.Length == 0)
                    {
                        m_btAryData[8] = 0x00;
                    }
                    else
                    {
                        m_btAryData[8] = Convert.ToByte(txtInterval.Text);
                    }

                    if (txtRepeat.Text.Length == 0)
                    {
                        m_btAryData[9] = 0x00;
                    }
                    else
                    {
                        m_btAryData[9] = Convert.ToByte(txtRepeat.Text);
                    }

                    m_nSwitchTotal = 0;
                    m_nSwitchTime = 0;
                    reader.FastSwitchInventory(m_curSetting.btReadId, m_btAryData);
                }
                else
                {
                    byte btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                    reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                }                
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }     */
        }

        private void ProcessFastSwitch(MessageTran msgTran)
        {
            var strCmd = "Real time inventory with fast ant switch";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                WriteLog(lrtxtLog, strLog, 1);
                RefreshFastSwitch(0x01);
                RunLoopFastSwitch();
            }
            else if (msgTran.AryData.Length == 2)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[1]);
                var strLog = strCmd + "Failure, failure cause: " + strErrorCode + "--" + "Antenna" +
                             (msgTran.AryData[0] + 1);

                WriteLog(lrtxtLog, strLog, 1);
            }

            else if (msgTran.AryData.Length == 7)
            {
                m_nSwitchTotal = Convert.ToInt32(msgTran.AryData[0]) * 255 * 255 +
                                 Convert.ToInt32(msgTran.AryData[1]) * 255 + Convert.ToInt32(msgTran.AryData[2]);
                m_nSwitchTime = Convert.ToInt32(msgTran.AryData[3]) * 255 * 255 * 255 +
                                Convert.ToInt32(msgTran.AryData[4]) * 255 * 255 +
                                Convert.ToInt32(msgTran.AryData[5]) * 255 + Convert.ToInt32(msgTran.AryData[6]);

                m_curInventoryBuffer.nDataCount = m_nSwitchTotal;
                m_curInventoryBuffer.nCommandDuration = m_nSwitchTime;
                WriteLog(lrtxtLog, strCmd, 0);
                RefreshFastSwitch(0x02);
                RunLoopFastSwitch();
            }
            /*else if (msgTran.AryData.Length == 8)
            {
                
                m_nSwitchTotal = Convert.ToInt32(msgTran.AryData[0]) * 255 * 255 * 255 + Convert.ToInt32(msgTran.AryData[1]) * 255 * 255 + Convert.ToInt32(msgTran.AryData[2]) * 255 + Convert.ToInt32(msgTran.AryData[3]);
                m_nSwitchTime = Convert.ToInt32(msgTran.AryData[4]) * 255 * 255 * 255 + Convert.ToInt32(msgTran.AryData[5]) * 255 * 255 + Convert.ToInt32(msgTran.AryData[6]) * 255 + Convert.ToInt32(msgTran.AryData[7]);

                m_curInventoryBuffer.nDataCount = m_nSwitchTotal;
                m_curInventoryBuffer.nCommandDuration = m_nSwitchTime;
                WriteLog(lrtxtLog, strCmd, 0);
                RefreshFastSwitch(0x02);
                RunLoopFastSwitch();
            }*/
            else
            {
                m_nTotal++;
                var nLength = msgTran.AryData.Length;
                var nEpcLength = nLength - 4;

                //Add inventory list
                var strEPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, nEpcLength);
                var strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, 2);
                var strRSSI = msgTran.AryData[nLength - 1].ToString();
                SetMaxMinRSSI(Convert.ToInt32(msgTran.AryData[nLength - 1]));
                var btTemp = msgTran.AryData[0];
                var btAntId = (byte) ((btTemp & 0x03) + 1);
                m_curInventoryBuffer.nCurrentAnt = btAntId;
                var strAntId = btAntId.ToString();
                var btFreq = (byte) (btTemp >> 2);

                var strFreq = GetFreqString(btFreq);

                var drs = m_curInventoryBuffer.dtTagTable.Select(string.Format("COLEPC = '{0}'", strEPC));
                if (drs.Length == 0)
                {
                    var row1 = m_curInventoryBuffer.dtTagTable.NewRow();
                    row1[0] = strPC;
                    row1[2] = strEPC;
                    row1[4] = strRSSI;
                    row1[5] = "1";
                    row1[6] = strFreq;
                    row1[7] = "0";
                    row1[8] = "0";
                    row1[9] = "0";
                    row1[10] = "0";
                    switch (btAntId)
                    {
                        case 0x01:
                        {
                            row1[7] = "1";
                        }
                            break;
                        case 0x02:
                        {
                            row1[8] = "1";
                        }
                            break;
                        case 0x03:
                        {
                            row1[9] = "1";
                        }
                            break;
                        case 0x04:
                        {
                            row1[10] = "1";
                        }
                            break;
                    }

                    m_curInventoryBuffer.dtTagTable.Rows.Add(row1);
                    m_curInventoryBuffer.dtTagTable.AcceptChanges();
                }
                else
                {
                    foreach (var dr in drs)
                    {
                        dr.BeginEdit();
                        var nTemp = 0;

                        dr[4] = strRSSI;
                        //dr[5] = (Convert.ToInt32(dr[5]) + 1).ToString();
                        nTemp = Convert.ToInt32(dr[5]);
                        dr[5] = (nTemp + 1).ToString();
                        dr[6] = strFreq;

                        switch (btAntId)
                        {
                            case 0x01:
                            {
                                //dr[7] = (Convert.ToInt32(dr[7]) + 1).ToString();
                                nTemp = Convert.ToInt32(dr[7]);
                                dr[7] = (nTemp + 1).ToString();
                            }
                                break;
                            case 0x02:
                            {
                                //dr[8] = (Convert.ToInt32(dr[8]) + 1).ToString();
                                nTemp = Convert.ToInt32(dr[8]);
                                dr[8] = (nTemp + 1).ToString();
                            }
                                break;
                            case 0x03:
                            {
                                //dr[9] = (Convert.ToInt32(dr[9]) + 1).ToString();
                                nTemp = Convert.ToInt32(dr[9]);
                                dr[9] = (nTemp + 1).ToString();
                            }
                                break;
                            case 0x04:
                            {
                                //dr[10] = (Convert.ToInt32(dr[10]) + 1).ToString();
                                nTemp = Convert.ToInt32(dr[10]);
                                dr[10] = (nTemp + 1).ToString();
                            }
                                break;
                        }

                        dr.EndEdit();
                    }

                    m_curInventoryBuffer.dtTagTable.AcceptChanges();
                }

                m_curInventoryBuffer.dtEndInventory = DateTime.Now;
                RefreshFastSwitch(0x00);
            }
        }

        private void ProcessInventoryReal(MessageTran msgTran)
        {
            var strCmd = "";
            if (msgTran.Cmd == 0x89) strCmd = "Real time inventory";
            if (msgTran.Cmd == 0x8B) strCmd = "User define Session and Inventoried Flag inventory";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                WriteLog(lrtxtLog, strLog, 1);
                RefreshInventoryReal(0x00);
                RunLoopInventroy();
            }
            else if (msgTran.AryData.Length == 7)
            {
                m_curInventoryBuffer.nReadRate =
                    Convert.ToInt32(msgTran.AryData[1]) * 256 + Convert.ToInt32(msgTran.AryData[2]);
                m_curInventoryBuffer.nDataCount = Convert.ToInt32(msgTran.AryData[3]) * 256 * 256 * 256 +
                                                  Convert.ToInt32(msgTran.AryData[4]) * 256 * 256 +
                                                  Convert.ToInt32(msgTran.AryData[5]) * 256 +
                                                  Convert.ToInt32(msgTran.AryData[6]);

                WriteLog(lrtxtLog, strCmd, 0);
                RefreshInventoryReal(0x01);
                RunLoopInventroy();
            }
            else
            {
                m_nTotal++;
                var nLength = msgTran.AryData.Length;
                var nEpcLength = nLength - 4;

                //Add inventory list
                //if (msgTran.AryData[3] == 0x00)
                //{
                //    MessageBox.Show("");
                //}
                var strEPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, nEpcLength);
                var strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, 2);
                var strRSSI = msgTran.AryData[nLength - 1].ToString();
                SetMaxMinRSSI(Convert.ToInt32(msgTran.AryData[nLength - 1]));
                var btTemp = msgTran.AryData[0];
                var btAntId = (byte) ((btTemp & 0x03) + 1);
                m_curInventoryBuffer.nCurrentAnt = btAntId;
                var strAntId = btAntId.ToString();

                var btFreq = (byte) (btTemp >> 2);
                var strFreq = GetFreqString(btFreq);

                //DataRow row = m_curInventoryBuffer.dtTagDetailTable.NewRow();
                //row[0] = strEPC;
                //row[1] = strRSSI;
                //row[2] = strAntId;
                //row[3] = strFreq;

                //m_curInventoryBuffer.dtTagDetailTable.Rows.Add(row);
                //m_curInventoryBuffer.dtTagDetailTable.AcceptChanges();

                ////Add tag list
                //DataRow[] drsDetail = m_curInventoryBuffer.dtTagDetailTable.Select(string.Format("COLEPC = '{0}'", strEPC));
                //int nDetailCount = drsDetail.Length;
                var drs = m_curInventoryBuffer.dtTagTable.Select(string.Format("COLEPC = '{0}'", strEPC));
                if (drs.Length == 0)
                {
                    var row1 = m_curInventoryBuffer.dtTagTable.NewRow();
                    row1[0] = strPC;
                    row1[2] = strEPC;
                    row1[4] = strRSSI;
                    row1[5] = "1";
                    row1[6] = strFreq;

                    m_curInventoryBuffer.dtTagTable.Rows.Add(row1);
                    m_curInventoryBuffer.dtTagTable.AcceptChanges();
                }
                else
                {
                    foreach (var dr in drs)
                    {
                        dr.BeginEdit();

                        dr[4] = strRSSI;
                        dr[5] = (Convert.ToInt32(dr[5]) + 1).ToString();
                        dr[6] = strFreq;

                        dr.EndEdit();
                    }

                    m_curInventoryBuffer.dtTagTable.AcceptChanges();
                }

                m_curInventoryBuffer.dtEndInventory = DateTime.Now;
                RefreshInventoryReal(0x89);
            }
        }


        private void ProcessInventory(MessageTran msgTran)
        {
            var strCmd = "Inventory";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 9)
            {
                m_curInventoryBuffer.nCurrentAnt = msgTran.AryData[0];
                m_curInventoryBuffer.nTagCount =
                    Convert.ToInt32(msgTran.AryData[1]) * 256 + Convert.ToInt32(msgTran.AryData[2]);
                m_curInventoryBuffer.nReadRate =
                    Convert.ToInt32(msgTran.AryData[3]) * 256 + Convert.ToInt32(msgTran.AryData[4]);
                var nTotalRead = Convert.ToInt32(msgTran.AryData[5]) * 256 * 256 * 256
                                 + Convert.ToInt32(msgTran.AryData[6]) * 256 * 256
                                 + Convert.ToInt32(msgTran.AryData[7]) * 256
                                 + Convert.ToInt32(msgTran.AryData[8]);
                m_curInventoryBuffer.nDataCount = nTotalRead;
                m_curInventoryBuffer.lTotalRead.Add(nTotalRead);
                m_curInventoryBuffer.dtEndInventory = DateTime.Now;

                RefreshInventory(0x80);
                WriteLog(lrtxtLog, strCmd, 0);

                RunLoopInventroy();

                return;
            }

            if (msgTran.AryData.Length == 1)
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            else
                strErrorCode = "Unknown Error";

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;
            WriteLog(lrtxtLog, strLog, 1);

            RunLoopInventroy();
        }

        private void btnGetInventoryBuffer_Click(object sender, EventArgs e)
        {
            m_curInventoryBuffer.dtTagTable.Rows.Clear();

            reader.GetInventoryBuffer(m_curSetting.btReadId);
        }

        private void SetMaxMinRSSI(int nRSSI)
        {
            if (m_curInventoryBuffer.nMaxRSSI < nRSSI) m_curInventoryBuffer.nMaxRSSI = nRSSI;

            if (m_curInventoryBuffer.nMinRSSI == 0)
                m_curInventoryBuffer.nMinRSSI = nRSSI;
            else if (m_curInventoryBuffer.nMinRSSI > nRSSI) m_curInventoryBuffer.nMinRSSI = nRSSI;
        }

        private void ProcessGetInventoryBuffer(MessageTran msgTran)
        {
            var strCmd = "Get buffered data without clearing";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                var nDataLen = msgTran.AryData.Length;
                var nEpcLen = Convert.ToInt32(msgTran.AryData[2]) - 4;

                var strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, 2);
                var strEpc = CCommondMethod.ByteArrayToString(msgTran.AryData, 5, nEpcLen);
                var strCRC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5 + nEpcLen, 2);
                var strRSSI = msgTran.AryData[nDataLen - 3].ToString();
                SetMaxMinRSSI(Convert.ToInt32(msgTran.AryData[nDataLen - 3]));
                var btTemp = msgTran.AryData[nDataLen - 2];
                var btAntId = (byte) ((btTemp & 0x03) + 1);
                var strAntId = btAntId.ToString();
                var strReadCnr = msgTran.AryData[nDataLen - 1].ToString();

                var row = m_curInventoryBuffer.dtTagTable.NewRow();
                row[0] = strPC;
                row[1] = strCRC;
                row[2] = strEpc;
                row[3] = strAntId;
                row[4] = strRSSI;
                row[5] = strReadCnr;

                m_curInventoryBuffer.dtTagTable.Rows.Add(row);
                m_curInventoryBuffer.dtTagTable.AcceptChanges();

                RefreshInventory(0x90);
                WriteLog(lrtxtLog, strCmd, 0);
            }
        }

        private void btnGetAndResetInventoryBuffer_Click(object sender, EventArgs e)
        {
            m_curInventoryBuffer.dtTagTable.Rows.Clear();

            reader.GetAndResetInventoryBuffer(m_curSetting.btReadId);
        }

        private void ProcessGetAndResetInventoryBuffer(MessageTran msgTran)
        {
            var strCmd = "Get and clear buffered data";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                var nDataLen = msgTran.AryData.Length;
                var nEpcLen = Convert.ToInt32(msgTran.AryData[2]) - 4;

                var strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, 2);
                var strEpc = CCommondMethod.ByteArrayToString(msgTran.AryData, 5, nEpcLen);
                var strCRC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5 + nEpcLen, 2);
                var strRSSI = msgTran.AryData[nDataLen - 3].ToString();
                SetMaxMinRSSI(Convert.ToInt32(msgTran.AryData[nDataLen - 3]));
                var btTemp = msgTran.AryData[nDataLen - 2];
                var btAntId = (byte) ((btTemp & 0x03) + 1);
                var strAntId = btAntId.ToString();
                var strReadCnr = msgTran.AryData[nDataLen - 1].ToString();

                var row = m_curInventoryBuffer.dtTagTable.NewRow();
                row[0] = strPC;
                row[1] = strCRC;
                row[2] = strEpc;
                row[3] = strAntId;
                row[4] = strRSSI;
                row[5] = strReadCnr;

                m_curInventoryBuffer.dtTagTable.Rows.Add(row);
                m_curInventoryBuffer.dtTagTable.AcceptChanges();

                RefreshInventory(0x91);
                WriteLog(lrtxtLog, strCmd, 0);
            }
        }

        private void btnGetInventoryBufferTagCount_Click(object sender, EventArgs e)
        {
            reader.GetInventoryBufferTagCount(m_curSetting.btReadId);
        }

        private void ProcessGetInventoryBufferTagCount(MessageTran msgTran)
        {
            var strCmd = "Query how many tags are buffered";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 2)
            {
                m_curInventoryBuffer.nTagCount =
                    Convert.ToInt32(msgTran.AryData[0]) * 256 + Convert.ToInt32(msgTran.AryData[1]);

                RefreshInventory(0x92);
                var strLog1 = strCmd + " " + m_curInventoryBuffer.nTagCount;
                WriteLog(lrtxtLog, strLog1, 0);
                return;
            }

            if (msgTran.AryData.Length == 1)
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            else
                strErrorCode = "Unknown Error";

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnResetInventoryBuffer_Click(object sender, EventArgs e)
        {
            reader.ResetInventoryBuffer(m_curSetting.btReadId);
        }

        private void ProcessResetInventoryBuffer(MessageTran msgTran)
        {
            var strCmd = "Clear buffer";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    RefreshInventory(0x93);
                    WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

            WriteLog(lrtxtLog, strLog, 1);
        }

        private void cbAccessEpcMatch_CheckedChanged(object sender, EventArgs e)
        {
            if (ckAccessEpcMatch.Checked)
            {
                reader.GetAccessEpcMatch(m_curSetting.btReadId);
            }
            else
            {
                m_curOperateTagBuffer.strAccessEpcMatch = "";
                txtAccessEpcMatch.Text = "";
                reader.CancelAccessEpcMatch(m_curSetting.btReadId, 0x01);
            }
        }

        private void ProcessGetAccessEpcMatch(MessageTran msgTran)
        {
            var strCmd = "Get selected tag";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x01)
                {
                    WriteLog(lrtxtLog, "Unselected Tag", 0);
                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                if (msgTran.AryData[0] == 0x00)
                {
                    m_curOperateTagBuffer.strAccessEpcMatch =
                        CCommondMethod.ByteArrayToString(msgTran.AryData, 2, Convert.ToInt32(msgTran.AryData[1]));

                    RefreshOpTag(0x86);
                    WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }

                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnSetAccessEpcMatch_Click(object sender, EventArgs e)
        {
            var reslut = CCommondMethod.StringToStringArray(cmbSetAccessEpcMatch.Text.ToUpper(), 2);

            if (reslut == null)
            {
                MessageBox.Show("Please select EPC number");
                return;
            }

            var btAryEpc = CCommondMethod.StringArrayToByteArray(reslut, reslut.Length);

            m_curOperateTagBuffer.strAccessEpcMatch = cmbSetAccessEpcMatch.Text;
            txtAccessEpcMatch.Text = cmbSetAccessEpcMatch.Text;
            ckAccessEpcMatch.Checked = true;
            reader.SetAccessEpcMatch(m_curSetting.btReadId, 0x00, Convert.ToByte(btAryEpc.Length), btAryEpc);
        }

        private void ProcessSetAccessEpcMatch(MessageTran msgTran)
        {
            var strCmd = "Select/Deselect Tag";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }

                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown Error";
            }

            var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

            WriteLog(lrtxtLog, strLog, 1);
        }

        private void btnReadTag_Click(object sender, EventArgs e)
        {
            try
            {
                byte btMemBank = 0x00;
                byte btWordAdd = 0x00;
                byte btWordCnt = 0x00;

                if (rdbReserved.Checked)
                {
                    btMemBank = 0x00;
                }
                else if (rdbEpc.Checked)
                {
                    btMemBank = 0x01;
                }
                else if (rdbTid.Checked)
                {
                    btMemBank = 0x02;
                }
                else if (rdbUser.Checked)
                {
                    btMemBank = 0x03;
                }
                else
                {
                    MessageBox.Show("Please select the area of tag");
                    return;
                }

                if (txtWordAdd.Text.Length != 0)
                {
                    btWordAdd = Convert.ToByte(txtWordAdd.Text);
                }
                else
                {
                    MessageBox.Show("Please select the start Add of tag");
                    return;
                }

                if (txtWordCnt.Text.Length != 0)
                {
                    btWordCnt = Convert.ToByte(txtWordCnt.Text);
                }
                else
                {
                    MessageBox.Show("Please select the Length");
                    return;
                }

                m_curOperateTagBuffer.dtTagTable.Clear();
                ltvOperate.Items.Clear();
                reader.ReadTag(m_curSetting.btReadId, btMemBank, btWordAdd, btWordCnt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ProcessReadTag(MessageTran msgTran)
        {
            var strCmd = "Read Tag";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                var nLen = msgTran.AryData.Length;
                var nDataLen = Convert.ToInt32(msgTran.AryData[nLen - 3]);
                var nEpcLen = Convert.ToInt32(msgTran.AryData[2]) - nDataLen - 4;

                var strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, 2);
                var strEPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5, nEpcLen);
                var strCRC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5 + nEpcLen, 2);
                var strData = CCommondMethod.ByteArrayToString(msgTran.AryData, 7 + nEpcLen, nDataLen);

                var byTemp = msgTran.AryData[nLen - 2];
                var byAntId = (byte) ((byTemp & 0x03) + 1);
                var strAntId = byAntId.ToString();

                var strReadCount = msgTran.AryData[nLen - 1].ToString();

                var row = m_curOperateTagBuffer.dtTagTable.NewRow();
                row[0] = strPC;
                row[1] = strCRC;
                row[2] = strEPC;
                row[3] = strData;
                row[4] = nDataLen.ToString();
                row[5] = strAntId;
                row[6] = strReadCount;

                m_curOperateTagBuffer.dtTagTable.Rows.Add(row);
                m_curOperateTagBuffer.dtTagTable.AcceptChanges();

                RefreshOpTag(0x81);
                WriteLog(lrtxtLog, strCmd, 0);
            }
        }

        private void btnWriteTag_Click(object sender, EventArgs e)
        {
            try
            {
                byte btMemBank = 0x00;
                byte btWordAdd = 0x00;
                byte btWordCnt = 0x00;

                if (rdbReserved.Checked)
                {
                    btMemBank = 0x00;
                }
                else if (rdbEpc.Checked)
                {
                    btMemBank = 0x01;
                }
                else if (rdbTid.Checked)
                {
                    btMemBank = 0x02;
                }
                else if (rdbUser.Checked)
                {
                    btMemBank = 0x03;
                }
                else
                {
                    MessageBox.Show("Please select the area of tag");
                    return;
                }

                if (txtWordAdd.Text.Length != 0)
                {
                    btWordAdd = Convert.ToByte(txtWordAdd.Text);
                }
                else
                {
                    MessageBox.Show("Pleader select the start Add of tag");
                    return;
                }

                var reslut = CCommondMethod.StringToStringArray(htxtReadAndWritePwd.Text.ToUpper(), 2);

                if (reslut == null)
                {
                    MessageBox.Show("Invalid input characters");
                    return;
                }

                if (reslut.GetLength(0) < 4)
                {
                    MessageBox.Show("Enter at least 4 bytes");
                    return;
                }

                var btAryPwd = CCommondMethod.StringArrayToByteArray(reslut, 4);

                reslut = CCommondMethod.StringToStringArray(htxtWriteData.Text.ToUpper(), 2);

                if (reslut == null)
                {
                    MessageBox.Show("Invalid input characters");
                    return;
                }

                var btAryWriteData = CCommondMethod.StringArrayToByteArray(reslut, reslut.Length);
                btWordCnt = Convert.ToByte(reslut.Length / 2 + reslut.Length % 2);

                txtWordCnt.Text = btWordCnt.ToString();

                m_curOperateTagBuffer.dtTagTable.Clear();
                ltvOperate.Items.Clear();
                reader.WriteTag(m_curSetting.btReadId, btAryPwd, btMemBank, btWordAdd, btWordCnt, btAryWriteData);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ProcessWriteTag(MessageTran msgTran)
        {
            var strCmd = "Write Tag";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                var nLen = msgTran.AryData.Length;
                var nEpcLen = Convert.ToInt32(msgTran.AryData[2]) - 4;

                if (msgTran.AryData[nLen - 3] != 0x10)
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[nLen - 3]);
                    var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                    WriteLog(lrtxtLog, strLog, 1);
                    return;
                }

                var strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, 2);
                var strEPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5, nEpcLen);
                var strCRC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5 + nEpcLen, 2);
                var strData = string.Empty;

                var byTemp = msgTran.AryData[nLen - 2];
                var byAntId = (byte) ((byTemp & 0x03) + 1);
                var strAntId = byAntId.ToString();

                var strReadCount = msgTran.AryData[nLen - 1].ToString();

                var row = m_curOperateTagBuffer.dtTagTable.NewRow();
                row[0] = strPC;
                row[1] = strCRC;
                row[2] = strEPC;
                row[3] = strData;
                row[4] = string.Empty;
                row[5] = strAntId;
                row[6] = strReadCount;

                m_curOperateTagBuffer.dtTagTable.Rows.Add(row);
                m_curOperateTagBuffer.dtTagTable.AcceptChanges();

                RefreshOpTag(0x82);
                WriteLog(lrtxtLog, strCmd, 0);
            }
        }

        private void btnLockTag_Click(object sender, EventArgs e)
        {
            byte btMemBank = 0x00;
            byte btLockType = 0x00;

            if (rdbAccessPwd.Checked)
            {
                btMemBank = 0x04;
            }
            else if (rdbKillPwd.Checked)
            {
                btMemBank = 0x05;
            }
            else if (rdbEpcMermory.Checked)
            {
                btMemBank = 0x03;
            }
            else if (rdbTidMemory.Checked)
            {
                btMemBank = 0x02;
            }
            else if (rdbUserMemory.Checked)
            {
                btMemBank = 0x01;
            }
            else
            {
                MessageBox.Show("Please select the protected area");
                return;
            }

            if (rdbFree.Checked)
            {
                btLockType = 0x00;
            }
            else if (rdbFreeEver.Checked)
            {
                btLockType = 0x02;
            }
            else if (rdbLock.Checked)
            {
                btLockType = 0x01;
            }
            else if (rdbLockEver.Checked)
            {
                btLockType = 0x03;
            }
            else
            {
                MessageBox.Show("Please select the type of protection");
                return;
            }

            var reslut = CCommondMethod.StringToStringArray(htxtLockPwd.Text.ToUpper(), 2);

            if (reslut == null)
            {
                MessageBox.Show("Invalid input characters");
                return;
            }

            if (reslut.GetLength(0) < 4)
            {
                MessageBox.Show("Enter at least 4 bytes");
                return;
            }

            var btAryPwd = CCommondMethod.StringArrayToByteArray(reslut, 4);

            m_curOperateTagBuffer.dtTagTable.Clear();
            ltvOperate.Items.Clear();
            reader.LockTag(m_curSetting.btReadId, btAryPwd, btMemBank, btLockType);
        }

        private void ProcessLockTag(MessageTran msgTran)
        {
            var strCmd = "Lock Tag";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                var nLen = msgTran.AryData.Length;
                var nEpcLen = Convert.ToInt32(msgTran.AryData[2]) - 4;

                if (msgTran.AryData[nLen - 3] != 0x10)
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[nLen - 3]);
                    var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                    WriteLog(lrtxtLog, strLog, 1);
                    return;
                }

                var strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, 2);
                var strEPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5, nEpcLen);
                var strCRC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5 + nEpcLen, 2);
                var strData = string.Empty;

                var byTemp = msgTran.AryData[nLen - 2];
                var byAntId = (byte) ((byTemp & 0x03) + 1);
                var strAntId = byAntId.ToString();

                var strReadCount = msgTran.AryData[nLen - 1].ToString();

                var row = m_curOperateTagBuffer.dtTagTable.NewRow();
                row[0] = strPC;
                row[1] = strCRC;
                row[2] = strEPC;
                row[3] = strData;
                row[4] = string.Empty;
                row[5] = strAntId;
                row[6] = strReadCount;

                m_curOperateTagBuffer.dtTagTable.Rows.Add(row);
                m_curOperateTagBuffer.dtTagTable.AcceptChanges();

                RefreshOpTag(0x83);
                WriteLog(lrtxtLog, strCmd, 0);
            }
        }

        private void btnKillTag_Click(object sender, EventArgs e)
        {
            var reslut = CCommondMethod.StringToStringArray(htxtKillPwd.Text.ToUpper(), 2);

            if (reslut == null)
            {
                MessageBox.Show("Invalid input characters");
                return;
            }

            if (reslut.GetLength(0) < 4)
            {
                MessageBox.Show("Enter at least 4 bytes");
                return;
            }

            var btAryPwd = CCommondMethod.StringArrayToByteArray(reslut, 4);

            m_curOperateTagBuffer.dtTagTable.Clear();
            ltvOperate.Items.Clear();
            reader.KillTag(m_curSetting.btReadId, btAryPwd);
        }

        private void ProcessKillTag(MessageTran msgTran)
        {
            var strCmd = "Kill Tag";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                var nLen = msgTran.AryData.Length;
                var nEpcLen = Convert.ToInt32(msgTran.AryData[2]) - 4;

                if (msgTran.AryData[nLen - 3] != 0x10)
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[nLen - 3]);
                    var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                    WriteLog(lrtxtLog, strLog, 1);
                    return;
                }

                var strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, 2);
                var strEPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5, nEpcLen);
                var strCRC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5 + nEpcLen, 2);
                var strData = string.Empty;

                var byTemp = msgTran.AryData[nLen - 2];
                var byAntId = (byte) ((byTemp & 0x03) + 1);
                var strAntId = byAntId.ToString();

                var strReadCount = msgTran.AryData[nLen - 1].ToString();

                var row = m_curOperateTagBuffer.dtTagTable.NewRow();
                row[0] = strPC;
                row[1] = strCRC;
                row[2] = strEPC;
                row[3] = strData;
                row[4] = string.Empty;
                row[5] = strAntId;
                row[6] = strReadCount;

                m_curOperateTagBuffer.dtTagTable.Rows.Add(row);
                m_curOperateTagBuffer.dtTagTable.AcceptChanges();

                RefreshOpTag(0x84);
                WriteLog(lrtxtLog, strCmd, 0);
            }
        }

        private void btnInventoryISO18000_Click(object sender, EventArgs e)
        {
            if (m_bContinue)
            {
                m_bContinue = false;
                btnInventoryISO18000.BackColor = Color.WhiteSmoke;
                btnInventoryISO18000.ForeColor = Color.Indigo;
                btnInventoryISO18000.Text = "Inventory";
            }
            else
            {
                //Judge whether EPC inventory is runing.
                if (m_curInventoryBuffer.bLoopInventory)
                {
                    if (MessageBox.Show("EPC C1G2 tag is inventoring, whether to stop?", "Prompt",
                            MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) ==
                        DialogResult.Cancel) return;

                    btnInventory_Click(sender, e);
                    return;
                }

                m_curOperateTagISO18000Buffer.ClearBuffer();
                ltvTagISO18000.Items.Clear();
                m_bContinue = true;
                btnInventoryISO18000.BackColor = Color.Indigo;
                btnInventoryISO18000.ForeColor = Color.White;
                btnInventoryISO18000.Text = "Stop";

                var strCmd = "Inventory";
                WriteLog(lrtxtLog, strCmd, 0);

                reader.InventoryISO18000(m_curSetting.btReadId);
            }
        }

        private void ProcessInventoryISO18000(MessageTran msgTran)
        {
            var strCmd = "Inventory";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] != 0xFF)
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                    var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                    WriteLog(lrtxtLog, strLog, 1);
                }
            }
            else if (msgTran.AryData.Length == 9)
            {
                var strAntID = CCommondMethod.ByteArrayToString(msgTran.AryData, 0, 1);
                var strUID = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, 8);

                //Add saved Tag List, no inventoried add recording, otherwise, the tag inventory number plus 1.
                var drs = m_curOperateTagISO18000Buffer.dtTagTable.Select(string.Format("UID = '{0}'", strUID));
                if (drs.Length == 0)
                {
                    var row = m_curOperateTagISO18000Buffer.dtTagTable.NewRow();
                    row[0] = strAntID;
                    row[1] = strUID;
                    row[2] = "1";
                    m_curOperateTagISO18000Buffer.dtTagTable.Rows.Add(row);
                    m_curOperateTagISO18000Buffer.dtTagTable.AcceptChanges();
                }
                else
                {
                    var row = drs[0];
                    row.BeginEdit();
                    row[2] = (Convert.ToInt32(row[2]) + 1).ToString();
                    m_curOperateTagISO18000Buffer.dtTagTable.AcceptChanges();
                }
            }
            else if (msgTran.AryData.Length == 2)
            {
                m_curOperateTagISO18000Buffer.nTagCnt = Convert.ToInt32(msgTran.AryData[1]);
                RefreshISO18000(msgTran.Cmd);

                //WriteLog(lrtxtLog, strCmd, 0);
            }
            else
            {
                strErrorCode = "Unknown Error";
                var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                WriteLog(lrtxtLog, strLog, 1);
            }
        }

        private void btnReadTagISO18000_Click(object sender, EventArgs e)
        {
            if (htxtReadUID.Text.Length == 0)
            {
                MessageBox.Show("Please enter UID");
                return;
            }

            if (htxtReadStartAdd.Text.Length == 0)
            {
                MessageBox.Show("Please enter Start Add");
                return;
            }

            if (txtReadLength.Text.Length == 0)
            {
                MessageBox.Show("Please enter Length");
                return;
            }

            var reslut = CCommondMethod.StringToStringArray(htxtReadUID.Text.ToUpper(), 2);

            if (reslut == null)
            {
                MessageBox.Show("Invalid input characters");
                return;
            }

            if (reslut.GetLength(0) < 8)
            {
                MessageBox.Show("Enter at least 8 bytes");
                return;
            }

            var btAryUID = CCommondMethod.StringArrayToByteArray(reslut, 8);

            reader.ReadTagISO18000(m_curSetting.btReadId, btAryUID, Convert.ToByte(htxtReadStartAdd.Text, 16),
                Convert.ToByte(txtReadLength.Text, 16));
        }

        private void ProcessReadTagISO18000(MessageTran msgTran)
        {
            var strCmd = "Read Tag";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                var strAntID = CCommondMethod.ByteArrayToString(msgTran.AryData, 0, 1);
                var strData = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, msgTran.AryData.Length - 1);

                m_curOperateTagISO18000Buffer.btAntId = Convert.ToByte(strAntID);
                m_curOperateTagISO18000Buffer.strReadData = strData;

                RefreshISO18000(msgTran.Cmd);

                WriteLog(lrtxtLog, strCmd, 0);
            }
        }

        private void btnWriteTagISO18000_Click(object sender, EventArgs e)
        {
            try
            {
                m_nLoopedTimes = 0;
                if (txtLoop.Text.Length == 0)
                    m_nLoopTimes = 0;
                else
                    m_nLoopTimes = Convert.ToInt32(txtLoop.Text);

                WriteTagISO18000();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void WriteTagISO18000()
        {
            if (htxtReadUID.Text.Length == 0)
            {
                MessageBox.Show("Please enter UID");
                return;
            }

            if (htxtWriteStartAdd.Text.Length == 0)
            {
                MessageBox.Show("Please enter Start Add");
                return;
            }

            if (htxtWriteData18000.Text.Length == 0)
            {
                MessageBox.Show("Please enter Data to be written");
                return;
            }

            var reslut = CCommondMethod.StringToStringArray(htxtReadUID.Text.ToUpper(), 2);

            if (reslut == null)
            {
                MessageBox.Show("Invalid input characters");
                return;
            }

            if (reslut.GetLength(0) < 8)
            {
                MessageBox.Show("Enter at least 8 bytes");
                return;
            }

            var btAryUID = CCommondMethod.StringArrayToByteArray(reslut, 8);

            var btStartAdd = Convert.ToByte(htxtWriteStartAdd.Text, 16);

            //string[] reslut = CCommondMethod.StringToStringArray(htxtWriteData18000.Text.ToUpper(), 2);
            var strTemp = cleanString(htxtWriteData18000.Text);
            reslut = CCommondMethod.StringToStringArray(strTemp.ToUpper(), 2);

            if (reslut == null)
            {
                MessageBox.Show("Invalid input characters");
                return;
            }

            var btAryData = CCommondMethod.StringArrayToByteArray(reslut, reslut.Length);

            //byte btLength = Convert.ToByte(txtWriteLength.Text, 16);
            var btLength = Convert.ToByte(reslut.Length);
            txtWriteLength.Text = string.Format("{0:X}", btLength);
            m_nBytes = reslut.Length;

            reader.WriteTagISO18000(m_curSetting.btReadId, btAryUID, btStartAdd, btLength, btAryData);
        }

        private string cleanString(string newStr)
        {
            var tempStr = newStr.Replace('\r', ' ');
            return tempStr.Replace('\n', ' ');
        }


        private void ProcessWriteTagISO18000(MessageTran msgTran)
        {
            var strCmd = "Write Tag";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                //string strAntID = CCommondMethod.ByteArrayToString(msgTran.AryData, 0, 1);
                //string strCnt = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, 1);

                m_curOperateTagISO18000Buffer.btAntId = msgTran.AryData[0];
                m_curOperateTagISO18000Buffer.btWriteLength = msgTran.AryData[1];

                //RefreshISO18000(msgTran.Cmd);
                var strLength = msgTran.AryData[1].ToString();
                var strLog = strCmd + ": " + "Successfully written" + strLength + "byte";
                WriteLog(lrtxtLog, strLog, 0);
                RunLoopISO18000(Convert.ToInt32(msgTran.AryData[1]));
            }
        }

        private void btnLockTagISO18000_Click(object sender, EventArgs e)
        {
            if (htxtReadUID.Text.Length == 0)
            {
                MessageBox.Show("Please enter UID");
                return;
            }

            if (htxtLockAdd.Text.Length == 0)
            {
                MessageBox.Show("Please enter write-protected Add");
                return;
            }

            //Confirm the write protection prompt
            if (MessageBox.Show("Are you sure to write protect this address permanently?", "Prompt",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) ==
                DialogResult.Cancel) return;

            var reslut = CCommondMethod.StringToStringArray(htxtReadUID.Text.ToUpper(), 2);

            if (reslut == null)
            {
                MessageBox.Show("Invalid input characters");
                return;
            }

            if (reslut.GetLength(0) < 8)
            {
                MessageBox.Show("Enter at least 8 bytes");
                return;
            }

            var btAryUID = CCommondMethod.StringArrayToByteArray(reslut, 8);

            var btStartAdd = Convert.ToByte(htxtLockAdd.Text, 16);

            reader.LockTagISO18000(m_curSetting.btReadId, btAryUID, btStartAdd);
        }

        private void ProcessLockTagISO18000(MessageTran msgTran)
        {
            var strCmd = "Permanent write protection";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                //string strAntID = CCommondMethod.ByteArrayToString(msgTran.AryData, 0, 1);
                //string strStatus = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, 1);

                m_curOperateTagISO18000Buffer.btAntId = msgTran.AryData[0];
                m_curOperateTagISO18000Buffer.btStatus = msgTran.AryData[1];

                //RefreshISO18000(msgTran.Cmd);
                var strLog = string.Empty;
                switch (msgTran.AryData[1])
                {
                    case 0x00:
                        strLog = strCmd + ": " + "Successfully locked";
                        break;
                    case 0xFE:
                        strLog = strCmd + ": " + "It is already locked state";
                        break;
                    case 0xFF:
                        strLog = strCmd + ": " + "Unable to lock";
                        break;
                }

                WriteLog(lrtxtLog, strLog, 0);
            }
        }

        private void btnQueryTagISO18000_Click(object sender, EventArgs e)
        {
            if (htxtReadUID.Text.Length == 0)
            {
                MessageBox.Show("Please enter UID");
                return;
            }

            if (htxtQueryAdd.Text.Length == 0)
            {
                MessageBox.Show("Please enter the query address");
                return;
            }

            var reslut = CCommondMethod.StringToStringArray(htxtReadUID.Text.ToUpper(), 2);

            if (reslut == null)
            {
                MessageBox.Show("Invalid input characters");
                return;
            }

            if (reslut.GetLength(0) < 8)
            {
                MessageBox.Show("Enter at least 8 bytes");
                return;
            }

            var btAryUID = CCommondMethod.StringArrayToByteArray(reslut, 8);

            var btStartAdd = Convert.ToByte(htxtQueryAdd.Text, 16);

            reader.QueryTagISO18000(m_curSetting.btReadId, btAryUID, btStartAdd);
        }

        private void ProcessQueryISO18000(MessageTran msgTran)
        {
            var strCmd = "Query Tag";
            var strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                var strLog = strCmd + "Failure, failure cause: " + strErrorCode;

                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                //string strAntID = CCommondMethod.ByteArrayToString(msgTran.AryData, 0, 1);
                //string strStatus = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, 1);

                m_curOperateTagISO18000Buffer.btAntId = msgTran.AryData[0];
                m_curOperateTagISO18000Buffer.btStatus = msgTran.AryData[1];

                RefreshISO18000(msgTran.Cmd);

                WriteLog(lrtxtLog, strCmd, 0);
            }
        }

        private void htxtSendData_Leave(object sender, EventArgs e)
        {
            if (htxtSendData.TextLength == 0) return;

            var reslut = CCommondMethod.StringToStringArray(htxtSendData.Text.ToUpper(), 2);
            var btArySendData = CCommondMethod.StringArrayToByteArray(reslut, reslut.Length);

            var btCheckData = reader.CheckValue(btArySendData);
            htxtCheckData.Text = string.Format(" {0:X2}", btCheckData);
        }

        private void btnSendData_Click(object sender, EventArgs e)
        {
            if (htxtSendData.TextLength == 0) return;

            var strData = htxtSendData.Text + htxtCheckData.Text;

            var reslut = CCommondMethod.StringToStringArray(strData.ToUpper(), 2);
            var btArySendData = CCommondMethod.StringArrayToByteArray(reslut, reslut.Length);

            reader.SendMessage(btArySendData);
        }

        private void btnClearData_Click(object sender, EventArgs e)
        {
            htxtSendData.Text = "";
            htxtCheckData.Text = "";
        }

        private void lrtxtDataTran_DoubleClick(object sender, EventArgs e)
        {
            lrtxtDataTran.Text = "";
        }

        private void lrtxtLog_DoubleClick(object sender, EventArgs e)
        {
            lrtxtLog.Text = "";
        }

        private void tabCtrMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_bLockTab) tabCtrMain.SelectTab(1);
            var nIndex = tabCtrMain.SelectedIndex;

            if (nIndex == 2)
            {
                lrtxtDataTran.Select(lrtxtDataTran.TextLength, 0);
                lrtxtDataTran.ScrollToCaret();
            }
        }

        private void txtTcpPort_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == (char) ConsoleKey.Backspace) e.Handled = false;
        }

        private void txtOutputPower_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == (char) ConsoleKey.Backspace) e.Handled = false;
        }

        private void txtChannel_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == (char) ConsoleKey.Backspace) e.Handled = false;
        }

        private void txtWordAdd_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == (char) ConsoleKey.Backspace) e.Handled = false;
        }

        private void txtWordCnt_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == (char) ConsoleKey.Backspace) e.Handled = false;
        }

        private void cmbSetAccessEpcMatch_DropDown(object sender, EventArgs e)
        {
            cmbSetAccessEpcMatch.Items.Clear();
            var drs = m_curInventoryBuffer.dtTagTable.Select();
            foreach (var row in drs) cmbSetAccessEpcMatch.Items.Add(row[2].ToString());
        }


        private void btnClearInventoryRealResult_Click(object sender, EventArgs e)
        {
            m_curInventoryBuffer.ClearInventoryRealResult();


            lvRealList.Items.Clear();
            //ltvInventoryTag.Items.Clear();
        }

        private void ltvInventoryEpc_SelectedIndexChanged(object sender, EventArgs e)
        {
            //ltvInventoryTag.Items.Clear();
            DataRow[] drs;

            if (lvRealList.SelectedItems.Count == 0)
                drs = m_curInventoryBuffer.dtTagDetailTable.Select();
            else
                foreach (ListViewItem itemEpc in lvRealList.SelectedItems)
                {
                    //ListViewItem itemEpc = ltvInventoryEpc.Items[nIndex];
                    var strEpc = itemEpc.SubItems[1].Text;

                    drs = m_curInventoryBuffer.dtTagDetailTable.Select(string.Format("COLEPC = '{0}'", strEpc));
                    //ShowListView(ltvInventoryTag, drs);
                }
        }

        private void ShowListView(ListView ltvListView, DataRow[] drSelect)
        {
            //ltvListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            var nItemCount = ltvListView.Items.Count;
            var nIndex = 1;

            foreach (var row in drSelect)
            {
                var item = new ListViewItem();
                item.Text = (nItemCount + nIndex).ToString();
                item.SubItems.Add(row[0].ToString());

                var strTemp = Convert.ToInt32(row[1].ToString()) - 129 + "dBm";
                item.SubItems.Add(strTemp);
                var byTemp = Convert.ToByte(row[1]);
                if (byTemp > 0x50)
                    item.BackColor = Color.PowderBlue;
                else if (byTemp < 0x30) item.BackColor = Color.LemonChiffon;

                item.SubItems.Add(row[2].ToString());
                item.SubItems.Add(row[3].ToString());

                ltvListView.Items.Add(item);
                ltvListView.Items[nIndex - 1].EnsureVisible();
                nIndex++;
            }
        }

        private void ltvTagISO18000_DoubleClick(object sender, EventArgs e)
        {
            //if (ltvTagISO18000.SelectedItems.Count == 1)
            //{
            //    ListViewItem item = ltvTagISO18000.SelectedItems[0];
            //    string strUID = item.SubItems[1].Text;
            //    htxtReadUID.Text = strUID;
            //}
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            htxtReadUID.Text = "";
            htxtReadStartAdd.Text = "";
            txtReadLength.Text = "";
            htxtReadData18000.Text = "";
            htxtWriteStartAdd.Text = "";
            txtWriteLength.Text = "";
            htxtWriteData18000.Text = "";
            htxtLockAdd.Text = "";
            htxtQueryAdd.Text = "";
            txtStatus.Text = "";
            txtLoop.Text = "1";
            ltvTagISO18000.Items.Clear();
        }

        private void ltvTagISO18000_Click(object sender, EventArgs e)
        {
            if (ltvTagISO18000.SelectedItems.Count == 1)
            {
                var item = ltvTagISO18000.SelectedItems[0];
                var strUID = item.SubItems[1].Text;
                htxtReadUID.Text = strUID;
            }
        }

        private void ckDisplayLog_CheckedChanged(object sender, EventArgs e)
        {
            if (ckDisplayLog.Checked)
                m_bDisplayLog = true;
            else
                m_bDisplayLog = false;
        }


        private void btRealTimeInventory_Click(object sender, EventArgs e)
        {
            try
            {
                cbWriteDB.Enabled = false;
                numericUpDown1.Enabled = false;
                m_curInventoryBuffer.ClearInventoryPar();

                if (textRealRound.Text.Length == 0)
                {
                    MessageBox.Show("Please enter the number of cycles");
                    return;
                }

                m_curInventoryBuffer.btRepeat = Convert.ToByte(textRealRound.Text);

                if (cbRealSession.Checked)
                {
                    if (cmbSession.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please enter Session ID");
                        return;
                    }

                    if (cmbTarget.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please enter Inventoried Flag");
                        return;
                    }

                    m_curInventoryBuffer.bLoopCustomizedSession = true;
                    m_curInventoryBuffer.btSession = (byte) cmbSession.SelectedIndex;
                    m_curInventoryBuffer.btTarget = (byte) cmbTarget.SelectedIndex;
                }
                else
                {
                    m_curInventoryBuffer.bLoopCustomizedSession = false;
                }

                if (cbRealWorkant1.Checked) m_curInventoryBuffer.lAntenna.Add(0x00);
                if (cbRealWorkant2.Checked) m_curInventoryBuffer.lAntenna.Add(0x01);
                if (cbRealWorkant3.Checked) m_curInventoryBuffer.lAntenna.Add(0x02);
                if (cbRealWorkant4.Checked) m_curInventoryBuffer.lAntenna.Add(0x03);
                if (m_curInventoryBuffer.lAntenna.Count == 0)
                {
                    MessageBox.Show("One antenna must be selected");
                    return;
                }

                //Default cycle to send commands
                if (m_curInventoryBuffer.bLoopInventory)
                {
                    m_bInventory = false;
                    m_curInventoryBuffer.bLoopInventory = false;
                    btRealTimeInventory.BackColor = Color.WhiteSmoke;
                    btRealTimeInventory.ForeColor = Color.DarkBlue;
                    btRealTimeInventory.Text = "Inventory";
                    timerInventory.Enabled = false;
                    cbWriteDB.Enabled = true;
                    numericUpDown1.Enabled = true;
                    return;
                }

                //Whether ISO 18000-6B Inventory is runing.
                if (m_bContinue)
                {
                    if (MessageBox.Show("ISO 18000-6B tag is inventoring, whether to stop?", "Prompt",
                            MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) ==
                        DialogResult.Cancel) return;

                    btnInventoryISO18000_Click(sender, e);
                    return;
                }

                m_bInventory = true;
                m_curInventoryBuffer.bLoopInventory = true;
                btRealTimeInventory.BackColor = Color.DarkBlue;
                btRealTimeInventory.ForeColor = Color.White;
                btRealTimeInventory.Text = "Stop";

                m_curInventoryBuffer.bLoopInventoryReal = true;

                m_curInventoryBuffer.ClearInventoryRealResult();
                lvRealList.Items.Clear();
                lvRealList.Items.Clear();
                tbRealMaxRssi.Text = "0";
                tbRealMinRssi.Text = "0";
                m_nTotal = 0;


                var btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                m_curSetting.btWorkAntenna = btWorkAntenna;

                timerInventory.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btRealFresh_Click(object sender, EventArgs e)
        {
            m_curInventoryBuffer.ClearInventoryRealResult();

            lvRealList.Items.Clear();
            lvRealList.Items.Clear();
            ledReal1.Text = "0";
            ledReal2.Text = "0";
            ledReal3.Text = "0";
            ledReal4.Text = "0";
            ledReal5.Text = "0";
            tbRealMaxRssi.Text = "0";
            tbRealMinRssi.Text = "0";
            textRealRound.Text = "1";
            cbRealWorkant1.Checked = true;
            cbRealWorkant2.Checked = false;
            cbRealWorkant3.Checked = false;
            cbRealWorkant4.Checked = false;
            lbRealTagCount.Text = "Tag List:";
        }

        private void btBufferInventory_Click(object sender, EventArgs e)
        {
            try
            {
                m_curInventoryBuffer.ClearInventoryPar();

                if (textReadRoundBuffer.Text.Length == 0)
                {
                    MessageBox.Show("Please enter the number of cycles");
                    return;
                }

                m_curInventoryBuffer.btRepeat = Convert.ToByte(textReadRoundBuffer.Text);

                if (cbBufferWorkant1.Checked) m_curInventoryBuffer.lAntenna.Add(0x00);
                if (cbBufferWorkant2.Checked) m_curInventoryBuffer.lAntenna.Add(0x01);
                if (cbBufferWorkant3.Checked) m_curInventoryBuffer.lAntenna.Add(0x02);
                if (cbBufferWorkant4.Checked) m_curInventoryBuffer.lAntenna.Add(0x03);
                if (m_curInventoryBuffer.lAntenna.Count == 0)
                {
                    MessageBox.Show("One antenna must be selected");
                    return;
                }


                //Default cycle to send commands
                if (m_curInventoryBuffer.bLoopInventory)
                {
                    m_bInventory = false;
                    m_curInventoryBuffer.bLoopInventory = false;
                    btBufferInventory.BackColor = Color.WhiteSmoke;
                    btBufferInventory.ForeColor = Color.DarkBlue;
                    btBufferInventory.Text = "Inventory";

                    return;
                }

                //Whether ISO 18000-6B Inventory is runing.
                if (m_bContinue)
                {
                    if (MessageBox.Show("ISO 18000-6B tag is inventoring, whether to stop?", "Prompt",
                            MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) ==
                        DialogResult.Cancel) return;

                    btnInventoryISO18000_Click(sender, e);
                    return;
                }

                m_bInventory = true;
                m_curInventoryBuffer.bLoopInventory = true;
                btBufferInventory.BackColor = Color.DarkBlue;
                btBufferInventory.ForeColor = Color.White;
                btBufferInventory.Text = "Stop";


                m_curInventoryBuffer.ClearInventoryRealResult();
                lvBufferList.Items.Clear();
                lvBufferList.Items.Clear();
                m_nTotal = 0;

                var btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                m_curSetting.btWorkAntenna = btWorkAntenna;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btGetBuffer_Click(object sender, EventArgs e)
        {
            m_curInventoryBuffer.dtTagTable.Rows.Clear();
            lvBufferList.Items.Clear();
            reader.GetInventoryBuffer(m_curSetting.btReadId);
        }

        private void btGetClearBuffer_Click(object sender, EventArgs e)
        {
            m_curInventoryBuffer.dtTagTable.Rows.Clear();
            lvBufferList.Items.Clear();
            reader.GetAndResetInventoryBuffer(m_curSetting.btReadId);
        }

        private void btClearBuffer_Click(object sender, EventArgs e)
        {
            reader.ResetInventoryBuffer(m_curSetting.btReadId);
            btBufferFresh_Click(sender, e);
        }

        private void btQueryBuffer_Click(object sender, EventArgs e)
        {
            reader.GetInventoryBufferTagCount(m_curSetting.btReadId);
        }

        private void btBufferFresh_Click(object sender, EventArgs e)
        {
            m_curInventoryBuffer.ClearInventoryRealResult();
            lvBufferList.Items.Clear();
            lvBufferList.Items.Clear();
            ledBuffer1.Text = "0";
            ledBuffer2.Text = "0";
            ledBuffer3.Text = "0";
            ledBuffer4.Text = "0";
            ledBuffer5.Text = "0";

            textReadRoundBuffer.Text = "1";
            cbBufferWorkant1.Checked = true;
            cbBufferWorkant2.Checked = false;
            cbBufferWorkant3.Checked = false;
            cbBufferWorkant4.Checked = false;
            labelBufferTagCount.Text = "Tag List:";
        }

        private void btFastInventory_Click(object sender, EventArgs e)
        {
            short antASelection = 1;
            short antBSelection = 1;
            short antCSelection = 1;
            short antDSelection = 1;
            //Default cycle to send commands
            if (m_curInventoryBuffer.bLoopInventory)
            {
                m_bInventory = false;
                m_curInventoryBuffer.bLoopInventory = false;
                btFastInventory.BackColor = Color.WhiteSmoke;
                btFastInventory.ForeColor = Color.DarkBlue;
                btFastInventory.Text = "Inventory";
                return;
            }

            //Whether ISO 18000-6B Inventory is runing.
            if (m_bContinue)
            {
                if (MessageBox.Show("ISO 18000-6B tag is inventoring, whether to stop?", "Prompt",
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) ==
                    DialogResult.Cancel) return;

                btnInventoryISO18000_Click(sender, e);
                return;
            }

            m_bInventory = true;
            m_curInventoryBuffer.bLoopInventory = true;
            btFastInventory.BackColor = Color.DarkBlue;
            btFastInventory.ForeColor = Color.White;
            btFastInventory.Text = "Stop";
            try
            {
                m_curInventoryBuffer.bLoopInventoryReal = true;

                m_curInventoryBuffer.ClearInventoryRealResult();
                lvFastList.Items.Clear();

                m_nTotal = 0;
                if (cmbAntSelect1.SelectedIndex < 0 || cmbAntSelect1.SelectedIndex > 3)
                    m_btAryData[0] = 0xFF;
                else
                    m_btAryData[0] = Convert.ToByte(cmbAntSelect1.SelectedIndex);
                if (txtAStay.Text.Length == 0)
                    m_btAryData[1] = 0x00;
                else
                    m_btAryData[1] = Convert.ToByte(txtAStay.Text);

                if (cmbAntSelect2.SelectedIndex < 0 || cmbAntSelect2.SelectedIndex > 3)
                    m_btAryData[2] = 0xFF;
                else
                    m_btAryData[2] = Convert.ToByte(cmbAntSelect2.SelectedIndex);
                if (txtBStay.Text.Length == 0)
                    m_btAryData[3] = 0x00;
                else
                    m_btAryData[3] = Convert.ToByte(txtBStay.Text);

                if (cmbAntSelect3.SelectedIndex < 0 || cmbAntSelect3.SelectedIndex > 3)
                    m_btAryData[4] = 0xFF;
                else
                    m_btAryData[4] = Convert.ToByte(cmbAntSelect3.SelectedIndex);
                if (txtCStay.Text.Length == 0)
                    m_btAryData[5] = 0x00;
                else
                    m_btAryData[5] = Convert.ToByte(txtCStay.Text);

                if (cmbAntSelect4.SelectedIndex < 0 || cmbAntSelect4.SelectedIndex > 3)
                    m_btAryData[6] = 0xFF;
                else
                    m_btAryData[6] = Convert.ToByte(cmbAntSelect4.SelectedIndex);
                if (txtDStay.Text.Length == 0)
                    m_btAryData[7] = 0x00;
                else
                    m_btAryData[7] = Convert.ToByte(txtDStay.Text);

                if (txtInterval.Text.Length == 0)
                    m_btAryData[8] = 0x00;
                else
                    m_btAryData[8] = Convert.ToByte(txtInterval.Text);

                if (txtRepeat.Text.Length == 0)
                    m_btAryData[9] = 0x00;
                else
                    m_btAryData[9] = Convert.ToByte(txtRepeat.Text);

                if (m_btAryData[0] > 3) antASelection = 0;

                if (m_btAryData[2] > 3) antBSelection = 0;

                if (m_btAryData[4] > 3) antCSelection = 0;

                if (m_btAryData[6] > 3) antDSelection = 0;

                if ((antASelection * m_btAryData[1] + antBSelection * m_btAryData[3] + antCSelection * m_btAryData[5] +
                     antDSelection * m_btAryData[7]) * m_btAryData[9] == 0)
                {
                    MessageBox.Show(
                        "One antenna must be selected, polling at least once,repeat per command at least once.");
                    m_bInventory = false;
                    m_curInventoryBuffer.bLoopInventory = false;
                    btFastInventory.BackColor = Color.WhiteSmoke;
                    btFastInventory.ForeColor = Color.DarkBlue;
                    btFastInventory.Text = "Inventory";
                    return;
                }

                m_nSwitchTotal = 0;
                m_nSwitchTime = 0;
                reader.FastSwitchInventory(m_curSetting.btReadId, m_btAryData);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void buttonFastFresh_Click(object sender, EventArgs e)
        {
            m_curInventoryBuffer.ClearInventoryRealResult();
            lvFastList.Items.Clear();
            lvFastList.Items.Clear();
            ledFast1.Text = "0";
            ledFast2.Text = "0";
            ledFast3.Text = "0";
            ledFast4.Text = "0";
            ledFast5.Text = "0";
            txtFastMinRssi.Text = "";
            txtFastMaxRssi.Text = "";
            txtFastTagList.Text = "Tag List:";

            cmbAntSelect1.SelectedIndex = 0;
            cmbAntSelect2.SelectedIndex = 1;
            cmbAntSelect3.SelectedIndex = 2;
            cmbAntSelect4.SelectedIndex = 3;

            txtAStay.Text = "1";
            txtBStay.Text = "1";
            txtCStay.Text = "1";
            txtDStay.Text = "1";

            txtInterval.Text = "0";
            txtRepeat.Text = "10";
        }

        private void pageFast4AntMode_Enter(object sender, EventArgs e)
        {
            buttonFastFresh_Click(sender, e);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            txtFirmwareVersion.Text = "";
            htxtReadId.Text = "";
            htbSetIdentifier.Text = "";
            txtReaderTemperature.Text = "";
            txtOutputPower.Text = "";
            htbGetIdentifier.Text = "";
        }

        private void btGetMonzaStatus_Click(object sender, EventArgs e)
        {
            reader.GetMonzaStatus(m_curSetting.btReadId);
        }

        private void btSetMonzaStatus_Click(object sender, EventArgs e)
        {
            byte btMonzaStatus = 0xFF;

            if (rdbMonzaOn.Checked)
                btMonzaStatus = 0x8D;
            else if (rdbMonzaOff.Checked)
                btMonzaStatus = 0x00;
            else
                return;

            reader.SetMonzaStatus(m_curSetting.btReadId, btMonzaStatus);
            m_curSetting.btMonzaStatus = btMonzaStatus;
        }

        private void btGetIdentifier_Click(object sender, EventArgs e)
        {
            reader.GetReaderIdentifier(m_curSetting.btReadId);
        }

        private void btSetIdentifier_Click(object sender, EventArgs e)
        {
            try
            {
                var strTemp = htbSetIdentifier.Text.Trim();


                var result = CCommondMethod.StringToStringArray(strTemp.ToUpper(), 2);

                if (result == null)
                {
                    MessageBox.Show("Invalid input characters");
                    return;
                }

                if (result.GetLength(0) != 12)
                {
                    MessageBox.Show("Please enter 12 bytes");
                    return;
                }

                var readerIdentifier = CCommondMethod.StringArrayToByteArray(result, 12);


                reader.SetReaderIdentifier(m_curSetting.btReadId, readerIdentifier);
                //m_curSetting.btReadId = Convert.ToByte(strTemp, 16);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btReaderSetupRefresh_Click(object sender, EventArgs e)
        {
            htxtReadId.Text = "";
            htbGetIdentifier.Text = "";
            htbSetIdentifier.Text = "";
            txtFirmwareVersion.Text = "";
            txtReaderTemperature.Text = "";
            rdbGpio1High.Checked = false;
            rdbGpio1Low.Checked = false;
            rdbGpio2High.Checked = false;
            rdbGpio2Low.Checked = false;
            rdbGpio3High.Checked = false;
            rdbGpio3Low.Checked = false;
            rdbGpio4High.Checked = false;
            rdbGpio4Low.Checked = false;

            rdbBeeperModeSlient.Checked = false;
            rdbBeeperModeInventory.Checked = false;
            rdbBeeperModeTag.Checked = false;

            cmbSetBaudrate.SelectedIndex = -1;
        }

        private void btRfSetup_Click(object sender, EventArgs e)
        {
            txtOutputPower.Text = "";
            cmbFrequencyStart.SelectedIndex = -1;
            cmbFrequencyEnd.SelectedIndex = -1;
            tbAntDectector.Text = "";

            rdbDrmModeOpen.Checked = false;
            rdbDrmModeClose.Checked = false;

            rdbMonzaOn.Checked = false;
            rdbMonzaOff.Checked = false;
            rdbRegionFcc.Checked = false;
            rdbRegionEtsi.Checked = false;
            rdbRegionChn.Checked = false;

            textReturnLoss.Text = "";
            cmbWorkAnt.SelectedIndex = -1;
            textStartFreq.Text = "";
            TextFreqInterval.Text = "";
            textFreqQuantity.Text = "";

            rdbProfile0.Checked = false;
            rdbProfile1.Checked = false;
            rdbProfile2.Checked = false;
            rdbProfile3.Checked = false;
        }

        private void cbRealSession_CheckedChanged(object sender, EventArgs e)
        {
            if (cbRealSession.Checked)
            {
                label97.Enabled = true;
                label98.Enabled = true;
                cmbSession.Enabled = true;
                cmbTarget.Enabled = true;
            }
            else
            {
                label97.Enabled = false;
                label98.Enabled = false;
                cmbSession.Enabled = false;
                cmbTarget.Enabled = false;
            }
        }

        private void btReturnLoss_Click(object sender, EventArgs e)
        {
            if (cmbReturnLossFreq.SelectedIndex != -1)
                reader.MeasureReturnLoss(m_curSetting.btReadId, Convert.ToByte(cmbReturnLossFreq.SelectedIndex));
        }

        private void cbUserDefineFreq_CheckedChanged(object sender, EventArgs e)
        {
            if (cbUserDefineFreq.Checked)
            {
                groupBox21.Enabled = false;
                groupBox23.Enabled = true;
            }
            else
            {
                groupBox21.Enabled = true;
                groupBox23.Enabled = false;
            }
        }

        private void btSetProfile_Click(object sender, EventArgs e)
        {
            byte btSelectedProfile = 0xFF;

            if (rdbProfile0.Checked)
                btSelectedProfile = 0xD0;
            else if (rdbProfile1.Checked)
                btSelectedProfile = 0xD1;
            else if (rdbProfile2.Checked)
                btSelectedProfile = 0xD2;
            else if (rdbProfile3.Checked)
                btSelectedProfile = 0xD3;
            else
                return;

            reader.SetRadioProfile(m_curSetting.btReadId, btSelectedProfile);
        }

        private void btGetProfile_Click(object sender, EventArgs e)
        {
            reader.GetRadioProfile(m_curSetting.btReadId);
        }

        private void tabCtrMain_Click(object sender, EventArgs e)
        {
            if (m_curSetting.btRegion < 1 || m_curSetting.btRegion > 4
            ) //If it is user defined frequencies, defined frequencies information need to be extracted firstly.
            {
                reader.GetFrequencyRegion(m_curSetting.btReadId);
                Thread.Sleep(5);
            }
        }

        private void timerInventory_Tick(object sender, EventArgs e)
        {
            m_nReceiveFlag++;
            if (m_nReceiveFlag >= 5)
            {
                RunLoopInventroy();
                m_nReceiveFlag = 0;
            }
        }

        private void rdbGpio4Low_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void label98_Click(object sender, EventArgs e)
        {
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
        }

        private delegate void WriteLogUnSafe(LogRichTextBox logRichTxt, string strLog, int nType);

        private delegate void RefreshInventoryUnsafe(byte btCmd);

        private delegate void RefreshOpTagUnsafe(byte btCmd);

        private delegate void RefreshInventoryRealUnsafe(byte btCmd);

        private delegate void RefreshFastSwitchUnsafe(byte btCmd);

        private delegate void RefreshReadSettingUnsafe(byte btCmd);

        private delegate void RunLoopInventoryUnsafe();

        private delegate void RunLoopFastSwitchUnsafe();

        private delegate void RefreshISO18000Unsafe(byte btCmd);

        private delegate void RunLoopISO18000Unsafe(int nLength);
    }
}