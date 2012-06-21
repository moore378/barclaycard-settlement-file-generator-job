using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using Common;
using TransactionManagementCommon;

namespace CctmForBlacklisted
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private CancellationTokenSource cancelToken;

        private void cancelJob()
        {
            if (cancelToken != null)
            {
                try
                {
                    cancelToken.Cancel();
                }
                finally
                {
                    cancelToken = null;
                }
            }
        }

        private static void findTransactionIndex(int transactionID, string ccTracks, DateTime dateTime, EncryptionMethod encryptionMethod, string terminalSerialNumber, int keyVersion, IEnumerable<int> transactionIndices, CancellationToken cancelToken, Action<int, string> success, Action<string> updateStatus)
        {
            updateStatus("Running");

            StripeDecryptor decryptor = new StripeDecryptor();

            var decodedStripe = DatabaseFormats.DecodeDatabaseStripe(ccTracks);

            Action cancelPoint = () => {if (cancelToken.IsCancellationRequested){ updateStatus("Cancelled"); cancelToken.ThrowIfCancellationRequested(); }};
            Func<int, UnencryptedStripe> decryptUsingIndex = (index) => decryptor.decryptStripe(decodedStripe, encryptionMethod, keyVersion, new TransactionInfo(dateTime, index, terminalSerialNumber, 0), "Err");

            Func<int, bool> indexPredicate = (index) =>
                {
                    cancelPoint();

                    if (index % 20 == 0)
                        updateStatus("Trying " + index.ToString());

                    try
                    {
                        var decryptedStripe = decryptUsingIndex(index);
                                                
                        // Check if valid decryption
                        if (decryptedStripe.Data.Substring(decryptedStripe.Data.Length - 4) != "\0\0\0\0")
                            throw new ValidationException("Invalid track");
                        var formattedStripe = TrackFormat.FormatSpecialStripeCases(decryptedStripe, encryptionMethod, "");
                        var stripe = new CreditCardStripe(formattedStripe);
                        var tracks = stripe.SplitIntoTracks("");
                        tracks.Validate("");
                        var trackTwo = tracks.ParseTrackTwo("");
                        trackTwo.Validate(""); 
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                };

            // Return the first index that works
            try
            {
                var workingIndex = transactionIndices.First(indexPredicate);

                var decryptedTrackTwo = new CreditCardStripe(TrackFormat.FormatSpecialStripeCases(decryptUsingIndex(workingIndex), encryptionMethod, "")).SplitIntoTracks("").TrackTwo.ToString();

                success(workingIndex, workingIndex.ToString() + " - " + decryptedTrackTwo);
            }
            catch (InvalidOperationException)
            {
                updateStatus("Not found");
            }
            catch (OperationCanceledException)
            {
                updateStatus("Cancelled");
            }
            catch
            {
                updateStatus("Unknown Error");
            }
        }

        private string calcTerminalSerialNumber(string uniqueRecordNo)
        {
            return uniqueRecordNo.Substring(3, 7);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            cancelJob();

            cancelToken = new CancellationTokenSource();

            var numbersToTry = Enumerable.Range(0, 10000);

            var updateStatus = new Func<DataSet1.TransactionFixesRow, Action<string>>( row =>
                {
                    return new Action<string>(status => Invoke(new Action(()=>row.Status = status)));
                });

            var success = new Func<DataSet1.TransactionFixesRow, DataSet1.SEL_TRANSACTIONRECORD_ERRSTATUS_TEMPRow, Action<int, string>>((fixRow, origRow) =>
                {
                    Action<int, string> closure = (transactionIndex, fix_info) => Invoke(new Action(() => 
                        { 
                            fixRow.Fix_Info = "Index = " + transactionIndex.ToString(); 
                            fixRow.Status = "Index found"; 
                            fixRow.Fix_Info = fix_info;
                        }));
                    return (closure);
                });

            this.sEL_TRANSACTIONRECORD_ERRSTATUS_TEMPTableAdapter.Fill(this.dataSet1.SEL_TRANSACTIONRECORD_ERRSTATUS_TEMP);
            this.dataSet1.TransactionFixes.Clear();
            var data = dataSet1.SEL_TRANSACTIONRECORD_ERRSTATUS_TEMP;
            foreach (var _row in data)
            {
                var row = _row; // Re-scope for closure
                var cancellationToken = cancelToken.Token;
                var updatedRow = dataSet1.TransactionFixes.AddTransactionFixesRow("Waiting", (int)row.TransactionRecordID, "");
                var task = new Task(() =>
                    {
                        try
                        {
                            findTransactionIndex((int)row.TransactionRecordID, row.CCTracks, row.StartDateTime, DatabaseFormats.decodeDatabaseEncryptionMethod(row.EncryptionVer),
                                calcTerminalSerialNumber(row.UniqueRecordNumber), (int)row.KeyVer, numbersToTry, cancellationToken, success(updatedRow, row), updateStatus(updatedRow));
                        }
                        catch (Exception er)
                        {
                            Invoke(new Action(()=>MessageBox.Show(er.Message)));
                        }
                    }, cancelToken.Token);
                task.Start();
                //task.RunSynchronously();
               
            }
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            cancelJob();
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            cancelJob();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var updTable = new DataSet1TableAdapters.QueriesTableAdapter();

            var data = dataSet1.TransactionFixes;
            var task = new Task(() =>
            {
                foreach (var row in data)
                {
                    Action<string> updateStatus = status => Invoke(new Action(() => row.Status = status));

                    if (row.Status == "Index found")
                    {
                        updateStatus("Upding DB..");
                        var newIndex = Int32.Parse(row.Fix_Info.Substring(0, row.Fix_Info.IndexOf(' ')));
                        Invoke(new Action(() => updTable.UPD_TRANSACTIONRECORD_CCINDEX_TEMP(row.TransactionRecordID, newIndex)));
                        Invoke(new Action(() => row.Fix_Info = "Updated CCTransactionIndex to " + newIndex.ToString()));
                        updateStatus("DB Updated");
                    }
                }
            });
            task.Start();
        }
    }
}
