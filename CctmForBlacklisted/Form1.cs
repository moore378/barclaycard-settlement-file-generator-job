using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TransactionManagementCommon;

namespace CctmForBlacklisted
{
    public partial class CctmForBlacklistedForm : Form
    {
        public CctmForBlacklistedForm()
        {
            InitializeComponent();
        }
               
        private void button1_Click(object sender, EventArgs e)
        {
            var updTable = new DataSet1TableAdapters.QueriesTableAdapter();

            this.sEL_DECLINED_TRANSACTIONRECORDS_CCTMTableAdapter.Fill(this.dataSet1.SEL_DECLINED_TRANSACTIONRECORDS_CCTM);

            var tableAdapter = new DataSet1TableAdapters.SEL_DECLINED_TRANSACTIONRECORDS_CCTMTableAdapter();
            var data = dataSet1.SEL_DECLINED_TRANSACTIONRECORDS_CCTM;
            for (int i = 0; i < data.Rows.Count; i++)
            {
                var record = new DeclinedRecord(
                    data[i].CCTracks,
                    new Transaction(data[i].StartDateTime, data[i].CCAmount), 
                    new Encoding((int)data[i].EncryptionVer, (int)data[i].CCTransactionIndex, (int)data[i].KeyVer),
                    new Source(data[i].TerminalSerNo));
                var updated = BlacklistedProcessor.SeparateTransaction(record);
                updTable.UPD_DECLINED_TRANSACTIONRECORDS_CCTM(data[i].TransactionRecordID, updated.Pan.FirstSixDigits, updated.Pan.LastFourDigits, updated.ExpDateYYMM, updated.Pan.Obscure(CreditCardPan.ObscurationMethod.Hash).ToString());
                dataSet1.SplitTransactions.AddSplitTransactionsRow(updated.Pan.FirstSixDigits, updated.Pan.LastFourDigits, updated.ExpDateYYMM, updated.Pan.Obscure(CreditCardPan.ObscurationMethod.Hash).ToString());
            }
        }

        private void CctmForBlacklistedForm_Load(object sender, EventArgs e)
        {
            
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            new Form2().Show();
        }
    }
}
