/*
 * Copyright 2010 Main Street Softworks, Inc. All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, are
 * permitted provided that the following conditions are met:
 * 
 *  1. Redistributions of source code must retain the above copyright notice, this list of
 *     conditions and the following disclaimer.
 *
 *  2. Redistributions in binary form must reproduce the above copyright notice, this list
 *     of conditions and the following disclaimer in the documentation and/or other materials
 *     provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY MAIN STREET SOFTWORKS INC ``AS IS'' AND ANY EXPRESS OR IMPLIED
 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
 * FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL MAIN STREET SOFTWORKS INC OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
 * ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 * The views and conclusions contained in the software and documentation are those of the
 * authors and should not be interpreted as representing official policies, either expressed
 * or implied, of Main Street Softworks, Inc.
 */

/* Overview:
 *    This library attempts to emulate the libmonetra C API as closely as possible.
 *    It provides 3 different methods of integration:
 *      1) API similar to the libmonetra C-API with one notable difference, that M_InitConn()
 *         returns an M_CONN class rather than passing in an object to be initialized.
 *      2) A true class-based implementation where-as the 'conn' isn't passed back to
 *         any of the functions, as a reference to it is contained within the initialized
 *         class.  So you could call Monetra conn = new Monetra(); conn.SetIP("localhost", 8333);
 *         Note the class methods remove the M_ prefix from the function names.
 *      3) A P/Invoke 'unsafe' emulation.  This should be a drop-in replacement for
 *         existing implementations which currently use the P/Invoke methods to the libmonetra.dll
 *
 *    This library is designed to be Thread-Safe, but has not yet been extensively tested
 *    as such.
 *
 * Building:
 *    - Requires .Net 2.0 or higher for some SSL calls
 *    - Needs references to System and System.Net
 *    - If building using 'mono' use 'gmcs' rather than 'mcs'
 *    - If you want the P/Invoke 'unsafe' API emulation, uncomment the #define USE_UNSAFE_API below
 *
 * Migration from P/Invoke libmonetra API notes:
 *  * Recommended migration away from 'unsafe' API.  Please see alternative method as well.
 *    - The connection pointer is no longer an IntPtr but rather an M_CONN class.
 *    - The connection pointer is no longer passed by reference (&) because classes are
 *      automatically passed by reference.
 *    - The connection pointer is now returned from M_InitConn() rather than passed in to
 *      be initialized.
 *    - The identifier returned by M_TransNew() is now of type 'int' rather than 'IntPtr'
 *    - M_SetDropFile() always returns false as it is unimplemented.
 *    - M_InitEngine() and M_DestroyEngine()s are No-Ops, they do not need to be called, but
 *      exist for compatibility purposes.
 *    - M_ValidateIdentifier() is a no-op, we always scan the hash table for a transaction
 *      as we are not returning direct pointers.
 *    - M_ResponseKeys() returns a string array, so there is no implementation for 
 *      M_ResponseKeys_index() or M_FreeResponseKeys() as they are not necessary.
 *    - M_TransBinaryKeyVal() takes a byte array rather than a string as an argument and does not
 *      take a length argument.
 *    - M_GetBinaryCell() returns a byte array rather than a string, it also does not take
 *      an outlen parameter to store the result length as it is not necessary.
 *  * Alternative Migration from P/Invoke methods.
 *    - #define USE_UNSAFE_API below and compile this as an 'unsafe' library.
 *    - add 'using libmonetra;' to the top of your project and include this library in your project.
 *    - Remove the old P/Invoke class definitions from your project.
 *    - Note: it is still necessary to call M_DestroyConn(), M_FreeResponseKeys(), etc to 
 *      clear up system resources when using the 'unsafe' methods since the class references
 *      are stored in a global Hashtable that the reference must be cleared.
 *
 * TODO:
 *    - Unit test
 *    - The 'cafile' specified by M_SetSSL_CAfile() is not honored, the default system
 *      certificate store is used instead.
 *    - The client certificates as provided by M_SetSSL_Files() are not currently honored.
 */

/* Notable references used in the creation of this library:
 *   TcpClient      : http://msdn.microsoft.com/en-us/library/system.net.sockets.tcpclient.aspx
 *   SSLStream      : http://msdn.microsoft.com/en-us/library/system.net.security.sslstream.aspx
 *   Socket         : http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.aspx
 *   Socket2Stream  : http://msdn.microsoft.com/en-us/library/system.net.sockets.networkstream.aspx
 *   Connect Timeout: http://channel9.msdn.com/forums/TechOff/41602-TcpClient-or-Socket-Connect-timeout/
 *   Data Available : http://msdn.microsoft.com/en-us/library/system.net.sockets.networkstream.dataavailable.aspx
 */


/* Whether or not to emulate the 'unsafe' API as used by the P/Invoke methods */
#define USE_UNSAFE_API

using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

//[assembly: AssemblyTitle("libmonetra")]
//[assembly: AssemblyDescription("Monetra .Net Interface Library")]
//[assembly: AssemblyConfiguration("")]
//[assembly: AssemblyCompany("Main Street Softworks, Inc")]
//[assembly: AssemblyProduct("libmonetra")]
//[assembly: AssemblyCopyright("Copyright � Main Street Softworks, Inc 2010")]
//[assembly: AssemblyTrademark("")]
//[assembly: AssemblyCulture("")]
//[assembly: ComVisible(true)]
//[assembly: Guid("a2dd3a40-8c9b-4f4b-82f4-0e902a6e0e21")]
//[assembly: AssemblyVersion("0.9.4.0")]
//[assembly: AssemblyFileVersion("0.9.4.0")]

namespace libmonetra {

/* Interface for COM */
public interface IMonetra
{
	/* CONSTANTS */
	int ERROR   { get; }
	int FAIL    { get; }
	int SUCCESS { get; }
	int DONE    { get; }
	int PENDING { get; }

	bool SetIP(string host, int port);
	bool SetSSL(string host, int port);
	bool SetDropFile(string directory);
	bool SetBlocking(bool tf);
	int TransNew();
	string ConnectionError();
	bool MaxConnTimeout(int secs);
	bool ValidateIdentifier(bool tf);
	bool VerifyConnection(bool tf);
	bool VerifySSLCert(bool tf);
	bool SetSSL_CAfile(string cafile);
	bool SetSSL_Files(string sslkeyfile, string sslcertfile);
	bool SetTimeout(int secs);
	bool TransKeyVal(int id, string key, string val);
	bool TransBinaryKeyVal(int id, string key, byte[] val);
	int CheckStatus(int id);
	bool DeleteTrans(int id);
	int CompleteAuthorizations(out int[] id_array);
	int TransInQueue();
	bool TransactionsSent();
	void uwait(int usec);
	string GetCell(int id, string col, int row);
	byte[] GetBinaryCell(int id, string col, int row);
	string GetCellByNum(int id, int col, int row);
	string GetCommaDelimited(int id);
	string GetHeader(int id, int col);
	bool IsCommaDelimited(int id);
	int NumColumns(int id);
	int NumRows(int id);
	string[] ResponseKeys(int id);
	string ResponseParam(int id, string key);
	int ReturnStatus(int id);
	bool Connect();
	void DestroyConn();
	bool TransSend(int id);
	bool Monitor();
	bool ParseCommaDelimited(int id);
}

public class Monetra : IMonetra {
	public const string version = "0.9.4";

	/* Base implementation, emulate our libmonetra API as closely as possible */

	private const int M_CONN_SSL = 1;
	private const int M_CONN_IP  = 2;

	private const int M_TRAN_STATUS_NEW  = 1;
	private const int M_TRAN_STATUS_SENT = 2;
	private const int M_TRAN_STATUS_DONE = 3;

	public const int M_ERROR   = -1;
	public const int M_FAIL    =  0;
	public const int M_SUCCESS =  1;

	public const int M_DONE    =  2;
	public const int M_PENDING =  3;

	private static string init_cafile = null;

	public class M_TRAN {
		public int         id;
		public int         status;
		public bool        comma_delimited;
		public Hashtable   in_params;
		public Hashtable   out_params;
		public byte[]      raw_response;
		public string[][]  csv;
	};
	
	public class M_CONN {
		public bool          blocking;
		public string        conn_error;
		public int           conn_timeout;
		public string        host;
		public int           last_id;
		public int           method;
		public int           port;
		public byte[]        readbuf;
		public string        ssl_cafile;
		public bool          ssl_verify;
		public string        ssl_cert;
		public string        ssl_key;
		public int           timeout;
		public Hashtable     tran_array;
		public bool          verify_conn;
		public byte[]        writebuf;
		public NetworkStream fd;
		public SslStream     ssl;
		public IAsyncResult  readresult;
		public byte[]        async_read_buf;
	};
	
	public static int M_InitEngine(string cafile)
	{
		init_cafile = cafile;
		return 1; /* Returning integer here for legacy reasons */
	}

	public static void M_DestroyEngine()
	{
		/* Do nothing */
	}

	public static M_CONN M_InitConn()
	{
		M_CONN conn = new M_CONN();
		conn.blocking        = false;
		conn.conn_error      = "";
		conn.conn_timeout    = 10;
		conn.host            = "";
		conn.last_id         = 0;
		conn.method          = M_CONN_IP;
		conn.port            = 0;
		conn.readbuf         = null;
		conn.ssl_cafile      = init_cafile;
		conn.ssl_verify      = false;
		conn.ssl_cert        = null;
		conn.timeout         = 0;
		conn.tran_array      = Hashtable.Synchronized(new Hashtable());
		conn.verify_conn     = true;
		conn.writebuf        = null;
		conn.readresult      = null;
		conn.async_read_buf  = new byte[8192];
		return conn;
	}
	
	public static bool M_SetIP(M_CONN conn, string host, int port)
	{
		conn.host   = host;
		conn.port   = port;
		conn.method = M_CONN_IP;
		
		return true;
	}
	
	
	public static bool M_SetSSL(M_CONN conn, string host, int port)
	{
		conn.host   = host;
		conn.port   = port;
		conn.method = M_CONN_SSL;
		
		return true;
	}
	
	
	public static bool M_SetDropFile(M_CONN conn, string directory)
	{
		/* NOT SUPPORTED */
		return false;
	}
	
	
	private static long time()
	{
		TimeSpan _TimeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
		return (long)_TimeSpan.TotalSeconds;
	}

	public static bool M_SetBlocking(M_CONN conn, bool tf)
	{
		conn.blocking = tf;

		return true;
	}
	
	
	public static int M_TransNew(M_CONN conn)
	{
		M_TRAN tran = new M_TRAN();
		
		tran.id              = Interlocked.Increment(ref conn.last_id);
		tran.status          = M_TRAN_STATUS_NEW;
		tran.comma_delimited = false;
		/* No reason for these to be synchronized, right? */
		tran.in_params       = new Hashtable();
		tran.out_params      = new Hashtable();
		tran.raw_response    = null;
		tran.csv             = null;
		
		conn.tran_array[tran.id] = tran;
		return tran.id;
	}
	
	
	private static bool M_verifyping(M_CONN conn)
	{
		bool blocking = conn.blocking;
		int id;
		
		M_SetBlocking(conn, false);

		id = M_TransNew(conn);
		M_TransKeyVal(conn, id, "action", "ping");
		
		if (!M_TransSend(conn, id)) {
			M_DeleteTrans(conn, id);
			return false;
		}
		
		long lasttime = time();
		while (M_CheckStatus(conn, id) == M_PENDING && time()-lasttime <= 5) {
			if (!M_Monitor(conn))
				break;
			M_uwait(10000);
		}
		
		M_SetBlocking(conn, blocking);

		int status = M_CheckStatus(conn, id);
		M_DeleteTrans(conn, id);
		if (status != M_DONE)
			return false;

		return true;
	}

	public static string M_ConnectionError(M_CONN conn)
	{
		return conn.conn_error;
	}
	

	public static bool M_MaxConnTimeout(M_CONN conn, int secs)
	{
		conn.conn_timeout = secs;
		return true;
	}
	
	
	public static bool M_ValidateIdentifier(M_CONN conn, bool tf)
	{
		/* Always validated, stub for compatibility */
		return true;
	}
	
	
	public static bool M_VerifyConnection(M_CONN conn, bool tf)
	{
		conn.verify_conn = tf;
		return true;
	}
	
	
	public static bool M_VerifySSLCert(M_CONN conn, bool tf)
	{
		conn.ssl_verify = tf;
		return true;
	}
	
	
	public static bool M_SetSSL_CAfile(M_CONN conn, string cafile)
	{
		conn.ssl_cafile = cafile;
		return true;
	}
	

	public static bool M_SetSSL_Files(M_CONN conn, string sslkeyfile, string sslcertfile)
	{
		if (sslkeyfile == null || sslkeyfile.Length == 0 || sslcertfile == null || sslcertfile.Length == 0)
			return false;
		
		conn.ssl_cert = sslcertfile;
		conn.ssl_key  = sslkeyfile;
		
		return true;
	}
	

	public static bool M_SetTimeout(M_CONN conn, int secs)
	{
		conn.timeout = secs;
		return true;
	}
	
	
	private static M_TRAN M_findtranbyid(M_CONN conn, int id)
	{
		if (!conn.tran_array.ContainsKey(id))
			return null;
		return (M_TRAN)conn.tran_array[id];
	}
	
	public static bool M_TransKeyVal(M_CONN conn, int id, string key, string val)
	{
		M_TRAN tran = M_findtranbyid(conn, id);
		/* Invalid ptr, or transaction has already been sent out */
		if (tran == null || tran.status != M_TRAN_STATUS_NEW)
			return false;
	
		tran.in_params[key] = val;
	
		return true;
	}
	
	
	public static bool M_TransBinaryKeyVal(M_CONN conn, int id, string key, byte[] val)
	{
		return M_TransKeyVal(conn, id, key, System.Convert.ToBase64String(val));
	}
	

	public static int M_CheckStatus(M_CONN conn, int id)
	{
		M_TRAN tran = M_findtranbyid(conn, id);
		if (tran == null ||
			(tran.status != M_TRAN_STATUS_SENT && tran.status != M_TRAN_STATUS_DONE))
			return M_ERROR;

		if (tran.status == M_TRAN_STATUS_SENT)
			return M_PENDING;

		return M_DONE;
	}
	
	
	public static bool M_DeleteTrans(M_CONN conn, int id)
	{
		M_TRAN tran = M_findtranbyid(conn, id);
		if (tran == null)
			return false;
		
		conn.tran_array.Remove(id);

		return true;
	}
	

	public static int M_CompleteAuthorizations(M_CONN conn, out int[] id_array)
	{
		id_array = new int[conn.tran_array.Count];
		int count = 0;
		foreach (DictionaryEntry kv in conn.tran_array) {
			id_array[count++] = (int)kv.Key;
		}
		return count;
	}
	
	
	public static int M_TransInQueue(M_CONN conn)
	{
		return conn.tran_array.Count;
	}
	

	public static bool M_TransactionsSent(M_CONN conn)
	{
		if (conn.writebuf.Length != 0)
			return false;
		return true;
	}
	
	
	public static void M_uwait(int usec)
	{
		System.Threading.Thread.Sleep(usec / 1000); 
	}
	
	
	public static string M_GetCell(M_CONN conn, int id, string col, int row)
	{
		M_TRAN tran = M_findtranbyid(conn, id);
		if (tran == null)
			return null;

		/* Invalid ptr, or transaction has not returned */
		if (tran.status != M_TRAN_STATUS_DONE)
			return null;
		if (!tran.comma_delimited)
			return null;
		if (row+1 >= tran.csv.Length)
			return null;
			
		for (int i=0; i<tran.csv[0].Length; i++) {
			if (String.Compare(tran.csv[0][i], col, true) == 0)
				return tran.csv[row+1][i];
		}
		return null;
	}
	

	public static byte[] M_GetBinaryCell(M_CONN conn, int id, string col, int row)
	{
		byte[] ret = null;
		
		string cell = M_GetCell(conn, id, col, row);
		if (cell != null)
			ret = System.Convert.FromBase64String(cell);
		return ret;
	}
	

	public static string M_GetCellByNum(M_CONN conn, int id, int col, int row)
	{
		M_TRAN tran = M_findtranbyid(conn, id);
		if (tran == null)
			return null;

		/* Invalid ptr, or transaction has not returned */
		if (tran.status != M_TRAN_STATUS_DONE)
			return null;
		if (!tran.comma_delimited)
			return null;
		if (row+1 >= tran.csv.Length || col >= tran.csv[0].Length)
			return null;
			
		return tran.csv[row+1][col];
	}
	

	public static string M_GetCommaDelimited(M_CONN conn, int id)
	{
		M_TRAN tran = M_findtranbyid(conn, id);
		if (tran == null)
			return null;

		/* Invalid ptr, or transaction has not returned */
		if (tran.status != M_TRAN_STATUS_DONE)
			return null;
			
		return Encoding.UTF8.GetString(tran.raw_response);
	}
	

	public static string M_GetHeader(M_CONN conn, int id, int col)
	{
		M_TRAN tran = M_findtranbyid(conn, id);
		if (tran == null)
			return null;

		/* Invalid ptr, or transaction has not returned */
		if (tran.status != M_TRAN_STATUS_DONE)
			return null;
		if (!tran.comma_delimited)
			return null;
		if (col >= tran.csv[0].Length)
			return null;

		return tran.csv[0][col];
	}


	public static bool M_IsCommaDelimited(M_CONN conn, int id)
	{
		M_TRAN tran = M_findtranbyid(conn, id);
		if (tran == null)
			return false;

		/* Invalid ptr, or transaction has not returned */
		if (tran.status != M_TRAN_STATUS_DONE)
			return false;
		return tran.comma_delimited;
	}


	public static int M_NumColumns(M_CONN conn, int id)
	{
		M_TRAN tran = M_findtranbyid(conn, id);
		if (tran == null)
			return 0;

		/* Invalid ptr, or transaction has not returned */
		if (tran.status != M_TRAN_STATUS_DONE)
			return 0;
		if (!tran.comma_delimited)
			return 0;
		return tran.csv[0].Length;
	}


	public static int M_NumRows(M_CONN conn, int id)
	{
		M_TRAN tran = M_findtranbyid(conn, id);
		if (tran == null)
			return 0;

		/* Invalid ptr, or transaction has not returned */
		if (tran.status != M_TRAN_STATUS_DONE)
			return 0;
		if (!tran.comma_delimited)
			return 0;
		return tran.csv.Length-1;
	}

	
	public static string[] M_ResponseKeys(M_CONN conn, int id)
	{
		M_TRAN tran = M_findtranbyid(conn, id);
		if (tran == null)
			return null;

		/* Invalid ptr, or transaction has not returned */
		if (tran.status != M_TRAN_STATUS_DONE)
			return null;

		string[] ret = new string[tran.out_params.Count];
		int count = 0;
		foreach (DictionaryEntry kv in tran.out_params) {
			ret[count++] = (string)kv.Key;
		}
		
		return ret;
	}

	public static string M_ResponseParam(M_CONN conn, int id, string key)
	{
		M_TRAN tran = M_findtranbyid(conn, id);
		if (tran == null)
			return null;

		/* Invalid ptr, or transaction has not returned */
		if (tran.status != M_TRAN_STATUS_DONE)
			return null;

		if (!tran.out_params.ContainsKey(key))
			return null;
		return (string)tran.out_params[key];
	}


	public static int M_ReturnStatus(M_CONN conn, int id)
	{
		M_TRAN tran = M_findtranbyid(conn, id);
		if (tran == null)
			return M_ERROR;

		/* Invalid ptr, or transaction has not returned */
		if (tran.status != M_TRAN_STATUS_DONE)
			return M_ERROR;

		if (tran.comma_delimited)
			return M_SUCCESS;

		string code = M_ResponseParam(conn, id, "code");
		if (String.Compare(code, "AUTH", true) == 0 || String.Compare(code, "SUCCESS", true) == 0)
			return M_SUCCESS;

		return M_FAIL;
	}

	private static bool ip_connect(M_CONN conn)
	{
	
#if !OLD_WAY
		Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		IAsyncResult result = socket.BeginConnect(conn.host, conn.port, null, null);
#else
		IPAddress ipAddress;
		try {
			ipAddress = Dns.Resolve(conn.host).AddressList[0];
		} catch (SocketException se) {
			conn.conn_error = "DNS failed: " + se.Message;
			return false;
		}
		IPEndPoint ipEndpoint = new IPEndPoint(ipAddress, conn.port);
		Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		IAsyncResult result = socket.BeginConnect(ipEndpoint, null, null);
#endif
		bool success = result.AsyncWaitHandle.WaitOne(conn.conn_timeout * 1000, true);

		if (!success) {
			conn.conn_error = "Connection Timeout";
			socket.Close();
			return false;
		}

		try {
			socket.EndConnect(result);
		} catch (SocketException se) {
			conn.conn_error = "Connection Failed: " + se.Message;
			socket.Close();
			return false;
		}

		conn.fd = new NetworkStream(socket, true);

		return true;
	}

	private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain,
		SslPolicyErrors sslPolicyErrors)
	{
		if (sslPolicyErrors == SslPolicyErrors.None)
			return true;
		return false;
	}

	private static bool DontValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain,
		SslPolicyErrors sslPolicyErrors)
	{
		return true;
	}

	public static bool M_Connect(M_CONN conn)
	{
		if (!ip_connect(conn))
			return false;

		if (conn.method == M_CONN_SSL) {
			RemoteCertificateValidationCallback rcvc = null;
			if (conn.ssl_verify) {
				rcvc = new RemoteCertificateValidationCallback(ValidateServerCertificate);
			} else {
				rcvc = new RemoteCertificateValidationCallback(DontValidateServerCertificate);
			}

			conn.ssl = new SslStream(conn.fd, true, rcvc);

			try {
				/* XXX: client certificates too */
				conn.ssl.AuthenticateAsClient(conn.host, null, SslProtocols.Tls, true);
			} catch (AuthenticationException e) {
				conn.conn_error = "SSL Exception: " + e.Message;
				closeConn(conn);
				return false;
			} catch (IOException e) {
				conn.conn_error = "SSL Exception: " + e.Message;
				closeConn(conn);
				return false; 
			}
		}


		if (conn.verify_conn && !M_verifyping(conn)) {
			conn.conn_error = "PING request failed";
			conn.fd.Close();
			conn.fd = null;
			return false;
		}

		return true;
	}

	private static void closeConn(M_CONN conn)
	{
		if (conn.method == M_CONN_SSL && conn.ssl != null) {
			conn.ssl.Close(); conn.ssl = null;
		}
		if (conn.fd != null) {
			conn.fd.Close(); conn.fd = null;
		}
	}

	public static void M_DestroyConn(M_CONN conn)
	{
		closeConn(conn);
		conn = null;
	}


	private static byte[] byteArrayConcat(byte[] str1, byte[] str2, int str2_len)
	{
		int str1_len;
		
		if (str1 == null)
			str1_len = 0;
		else
			str1_len = str1.Length;

		if (str2_len == -1) {
			if (str2 == null)
				str2_len = 0;
			else
				str2_len = str2.Length;
		}

		if (str1_len + str2_len == 0)
			return null;
			
		byte[] ret = new byte[str1_len + str2_len];
		if (str1_len > 0)
			System.Buffer.BlockCopy(str1, 0, ret, 0, str1_len);
		if (str2_len > 0)
			System.Buffer.BlockCopy(str2, 0, ret, str1_len, str2_len);
		return ret;
	}

	private static int byteArrayChr(byte[] str, byte chr)
	{
		if (str == null || str.Length == 0)
			return -1;
		for (int i=0; i < str.Length; i++)
			if (str[i] == chr)
				return i;
		return -1;
	}

	private static byte[] byteArraySubStr(byte[] str, int start, int length)
	{
		if (str == null || str.Length < length)
			return null;

		byte[] ret = new byte[length];
		System.Buffer.BlockCopy(str, start, ret, 0, length);
		return ret;
	}

	private static byte[] byteArrayTrim(byte[] str)
	{
		string mystr = Encoding.UTF8.GetString(str).Trim();
		return Encoding.UTF8.GetBytes(mystr);
	}

	public static bool M_TransSend(M_CONN conn, int id)
	{
		M_TRAN tran = M_findtranbyid(conn, id);

		/* Invalid ptr, or transaction has already been sent out */
		if (tran == null || tran.status != M_TRAN_STATUS_NEW)
			return false;

		tran.status = M_TRAN_STATUS_SENT;

		/* Structure Transaction */
		string tran_str;
		tran_str = "\x02" + tran.id.ToString() + "\x1c";

		/* PING is specially formed */
		if (tran.in_params.ContainsKey("action") &&
			String.Compare((String)tran.in_params["action"], "ping", true) == 0) {
			tran_str += "PING";
		} else {
			/* Each key/value pair in array as key="value" */
			foreach (DictionaryEntry kv in tran.in_params) {
				tran_str += (String)kv.Key + "=\"" + ((String)kv.Value).Replace("\"", "\"\"") + "\"\r\n"; 
			}
			/* Add timeout if necessary */
			if (conn.timeout != 0) {
				tran_str += "timeout=" + conn.timeout.ToString() + "\r\n";
			}
		}

		/* ETX */
		tran_str += "\x03";

		lock(conn) {
			/* Expand byte array to append new transaction */
			conn.writebuf = byteArrayConcat(conn.writebuf, Encoding.UTF8.GetBytes(tran_str), -1);
		}
		
		if (conn.blocking) {
			while (M_CheckStatus(conn, id) == M_PENDING) {
				if (!M_Monitor(conn))
					return false;
				M_uwait(20000);
			}
		}

		return true;
	}

	private static bool M_verify_comma_delimited(byte[] data)
	{
		for (int i=0; i<data.Length; i++) {
			/* If hit a new line or a comma before an equal sign, must
			 * be comma delimited */
			if (data[i] == 0x0A ||
				data[i] == 0x0D ||
				data[i] == ',')
				return true;

			/* If hit an equal sign before a new line or a comma, must be
			 * key/val */
			if (data[i] == '=')
				return false;
		}
		/* Who knows?  Should never get here */
		return true;
	}

	private static byte[][] byteArrayExplode(byte delim, byte[] data, byte quote_char, int max_sects)
	{
		bool on_quote;
		int beginsect;
		if (data == null || data.Length == 0)
			return null;

		/* We need to first count how many lines we have */
		on_quote  = false;
		beginsect = 0;
		int num_lines = 0;
		for (int i=0; i<data.Length; i++) {
			if (quote_char != 0 && data[i] == quote_char) {
				/* Doubling the quote char acts as escaping */
				if (data[i+1] == quote_char) {
					i++;
					continue;
				} else if (on_quote) {
					on_quote = false;
				} else {
					on_quote = true;
				}
			}
			if (data[i] == delim && !on_quote) {
				num_lines++;
				beginsect = i+1;
				if (max_sects != 0 && num_lines == max_sects-1)
					break;
			}
		}
		if (beginsect < data.Length)
			num_lines++;

		byte[][] ret = new byte[num_lines][];
		beginsect     = 0;
		int cnt       = 0;
		on_quote      = false;

		for (int i=0; i<data.Length; i++) {
			if (quote_char != 0 && data[i] == quote_char) {
				/* Doubling the quote char acts as escaping */
				if (data[i+1] == quote_char) {
					i++;
					continue;
				} else if (on_quote) {
					on_quote = false;
				} else {
					on_quote = true;
				}
			}
			if (data[i] == delim && !on_quote) {
				ret[cnt++] = byteArraySubStr(data, beginsect, i - beginsect);
				beginsect = i + 1;
				if (max_sects != 0 && cnt == max_sects - 1)
					break;
			}
		}
		if (beginsect < data.Length) {
			ret[cnt++] = byteArraySubStr(data, beginsect, data.Length - beginsect);
		}
		return ret;
	}

	private static string M_remove_dupe_quotes(byte[] str)
	{
		/* No quotes */
		if (byteArrayChr(str, 0x22) == -1)
			return Encoding.UTF8.GetString(str);

		StringBuilder mystr = new StringBuilder("");
		for (int i=0; i<str.Length; i++) {
			if (str[i] == 0x22 && i < str.Length-1 && str[i+1] == 0x22) {
				byte[] val = new byte[1] {0x22};
				mystr.Append(Encoding.UTF8.GetString(val));
				i++;
			} else if (str[i] != 0x22) {
				byte[] val = new byte[1] {str[i]};
				mystr.Append(Encoding.UTF8.GetString(val));
			}
		}
		return mystr.ToString();
	}

	private static bool M_Monitor_read(M_CONN conn)
	{
		/* Read Data */
		int bytes_read   = 0;
		do {
			try {
				if (conn.readresult == null) {
					if (conn.method == M_CONN_IP) {
						conn.readresult = conn.fd.BeginRead(conn.async_read_buf, 0, conn.async_read_buf.Length, null, null);
					} else if (conn.method == M_CONN_SSL) {
						conn.readresult = conn.ssl.BeginRead(conn.async_read_buf, 0, conn.async_read_buf.Length, null, null);
					}
				}
			
				if (!conn.readresult.IsCompleted) {
					break;
				}
				
				if (conn.method == M_CONN_IP) {
					bytes_read = conn.fd.EndRead(conn.readresult);
				} else if (conn.method == M_CONN_SSL) {
					bytes_read = conn.ssl.EndRead(conn.readresult);
				}
			} catch (IOException e) {
				conn.conn_error = "read failure: " + e.Message;
				closeConn(conn);
				conn.readresult = null;
				return false;
			}		
			conn.readresult = null;

			if (bytes_read == 0) {
				conn.conn_error = "read failure: remote disconnect";
				closeConn(conn);
				return false;
			}

			/* Append Data */
			conn.readbuf = byteArrayConcat(conn.readbuf, conn.async_read_buf, bytes_read);
		} while(bytes_read == conn.async_read_buf.Length);
		
		return true;
	}

	private static bool M_Monitor_write(M_CONN conn)
	{
		/* Write Data */
		if (conn.writebuf != null && conn.writebuf.Length > 0) {
			try {
				if (conn.method == M_CONN_IP) {
					conn.fd.Write(conn.writebuf, 0, conn.writebuf.Length);
				}
				if (conn.method == M_CONN_SSL) {
					conn.ssl.Write(conn.writebuf, 0, conn.writebuf.Length);
				}
			} catch (IOException e) {
				conn.conn_error = "write failure: " + e.Message;
				closeConn(conn);
				return false;
			}
			conn.writebuf = null;
		}
		return true;
	}
	
	private static bool M_Monitor_parse(M_CONN conn)
	{
		/* Parse */
		while(conn.readbuf != null && conn.readbuf.Length > 0) {
			if (conn.readbuf[0] != 0x02) {
				closeConn(conn);
				conn.conn_error = "protocol error, responses must start with STX";
				return false;
			}

			int etx = byteArrayChr(conn.readbuf, 0x03);
			if (etx == -1) {
				/* Not enough data */
				break;
			}

			
			/* Chop off txn from readbuf and copy it into txndata */
			byte[] txndata = byteArraySubStr(conn.readbuf, 0, etx);
			if (etx+1 == conn.readbuf.Length) {
				conn.readbuf = null;
			} else {
				conn.readbuf = byteArraySubStr(conn.readbuf, etx+1, conn.readbuf.Length-(etx+1));
			}

			int fs = byteArrayChr(txndata, 0x1c);
			if (fs == -1) {
				closeConn(conn);
				conn.conn_error = "protocol error, responses must contain a FS";
				return false;
			}

			byte[] id   = byteArraySubStr(txndata, 1, fs - 1);
			byte[] data = byteArraySubStr(txndata, fs+1, txndata.Length - fs - 1);

			M_TRAN txn = M_findtranbyid(conn, Convert.ToInt32(Encoding.UTF8.GetString(id)));
			if (txn == null) {
				/* Discarding data */
				continue;
			}

			txn.comma_delimited     = M_verify_comma_delimited(data);
			txn.raw_response        = data;
			data = null;

			if (!txn.comma_delimited) {
				byte[][] lines = byteArrayExplode(0x0A, txn.raw_response, 0x22, 0);

				if (lines == null || lines.Length == 0) {
					closeConn(conn);
					conn.conn_error = "protocol error, response contained no lines";
					return false;
				}

				for (int i=0; i<lines.Length; i++) {
					lines[i] = byteArrayTrim(lines[i]);
					if (lines[i] == null || lines[i].Length == 0)
						continue;

					byte[][] keyval = byteArrayExplode(0x3D, lines[i], 0, 2);

					if (keyval == null || keyval.Length != 2)
						continue;

					string key = Encoding.UTF8.GetString(keyval[0]);
					if (key == null || key.Length == 0)
						continue;
					txn.out_params[key] = M_remove_dupe_quotes(byteArrayTrim(keyval[1]));
				}
			}
			txn.status              = M_TRAN_STATUS_DONE;
		}
	
		return true;
	}

	public static bool M_Monitor(M_CONN conn)
	{
		lock(conn) {
			if (conn.fd == null)
				return false;

			if (!M_Monitor_read(conn))
				return false;

			if (!M_Monitor_write(conn))
				return false;
			
			if (!M_Monitor_parse(conn))
				return false;
		}
		return true;
	}
	
	
	private static string[][] M_parsecsv(byte[] data, byte delimiter, byte enclosure)
	{
		byte[][] lines = byteArrayExplode(0x0A, data, enclosure, 0);
		string[][] csv = new string[lines.Length][];
		for (int i=0; i<lines.Length; i++) {
			byte[][] cells = byteArrayExplode(delimiter, lines[i], enclosure, 0);
			csv[i] = new string[cells.Length];
			for (int j=0; j<cells.Length; j++) {
				csv[i][j] = M_remove_dupe_quotes(byteArrayTrim(cells[j]));
			}
		}
		return csv;
	}

	public static bool M_ParseCommaDelimited(M_CONN conn, int id)
	{
		M_TRAN tran = M_findtranbyid(conn, id);
		if (tran == null)
			return false;

		/* Invalid ptr, or transaction has not returned */
		if (tran.status != M_TRAN_STATUS_DONE)
			return false;

		tran.csv = M_parsecsv(tran.raw_response, 0x2c, 0x22);
		return true;
	}
	
	/* Make a wrapper for a more conventional class structure */
	private M_CONN classconn;
	public Monetra()
	{
		classconn = M_InitConn();
	}
	public bool SetIP(string host, int port)
	{
		return M_SetIP(classconn, host, port);
	}
	public bool SetSSL(string host, int port)
	{
		return M_SetSSL(classconn, host, port);
	}
	public bool SetDropFile(string directory)
	{
		return M_SetDropFile(classconn, directory);
	}
	public bool SetBlocking(bool tf)
	{
		return M_SetBlocking(classconn, tf);
	}
	public int TransNew()
	{
		return M_TransNew(classconn);
	}
	public string ConnectionError()
	{
		return M_ConnectionError(classconn);
	}
	public bool MaxConnTimeout(int secs)
	{
		return M_MaxConnTimeout(classconn, secs);
	}
	public bool ValidateIdentifier(bool tf)
	{
		return M_ValidateIdentifier(classconn, tf);
	}
	public bool VerifyConnection(bool tf)
	{
		return M_VerifyConnection(classconn, tf);
	}
	public bool VerifySSLCert(bool tf)
	{
		return M_VerifySSLCert(classconn, tf);
	}
	public bool SetSSL_CAfile(string cafile)
	{
		return M_SetSSL_CAfile(classconn, cafile);
	}
	public bool SetSSL_Files(string sslkeyfile, string sslcertfile)
	{
		return M_SetSSL_Files(classconn, sslkeyfile, sslcertfile);
	}
	public bool SetTimeout(int secs)
	{
		return M_SetTimeout(classconn, secs);
	}
	public bool TransKeyVal(int id, string key, string val)
	{
		return M_TransKeyVal(classconn, id, key, val);
	}
	public bool TransBinaryKeyVal(int id, string key, byte[] val)
	{
		return M_TransBinaryKeyVal(classconn, id, key, val);
	}
	public int CheckStatus(int id)
	{
		return M_CheckStatus(classconn, id);
	}
	public bool DeleteTrans(int id)
	{
		return M_DeleteTrans(classconn, id);
	}
	public int CompleteAuthorizations(out int[] id_array)
	{
		return M_CompleteAuthorizations(classconn, out id_array);
	}
	public int TransInQueue()
	{
		return M_TransInQueue(classconn);
	}
	public bool TransactionsSent()
	{
		return M_TransactionsSent(classconn);
	}
	public void uwait(int usec)
	{
		M_uwait(usec);
	}
	public string GetCell(int id, string col, int row)
	{
		return M_GetCell(classconn, id, col, row);
	}
	public byte[] GetBinaryCell(int id, string col, int row)
	{
		return M_GetBinaryCell(classconn, id, col, row);
	}
	public string GetCellByNum(int id, int col, int row)
	{
		return M_GetCellByNum(classconn, id, col, row);
	}
	public string GetCommaDelimited(int id)
	{
		return M_GetCommaDelimited(classconn, id);
	}
	public string GetHeader(int id, int col)
	{
		return M_GetHeader(classconn, id, col);
	}
	public bool IsCommaDelimited(int id)
	{
		return M_IsCommaDelimited(classconn, id);
	}
	public int NumColumns(int id)
	{
		return M_NumColumns(classconn, id);
	}
	public int NumRows(int id)
	{
		return M_NumRows(classconn, id);
	}
	public string[] ResponseKeys(int id)
	{
		return M_ResponseKeys(classconn, id);
	}
	public string ResponseParam(int id, string key)
	{
		return M_ResponseParam(classconn, id, key);
	}
	public int ReturnStatus(int id)
	{
		return M_ReturnStatus(classconn, id);
	}
	public bool Connect()
	{
		return M_Connect(classconn);
	}
	public void DestroyConn()
	{
		M_DestroyConn(classconn);
		classconn = null;
	}
	public bool TransSend(int id)
	{
		return M_TransSend(classconn, id);
	}
	public bool Monitor()
	{
		return M_Monitor(classconn);
	}
	public bool ParseCommaDelimited(int id)
	{
		return M_ParseCommaDelimited(classconn, id);
	}
	/* CONSTANTS */
	public int ERROR   { get { return M_ERROR;   } }
	public int FAIL    { get { return M_FAIL;    } }
	public int SUCCESS { get { return M_SUCCESS; } }
	public int DONE    { get { return M_DONE;    } }
	public int PENDING { get { return M_PENDING; } }

#if USE_UNSAFE_API
	/* Implementation of emulation for unsafe P/Invoke functions */
	private static int unsafe_last_id        = 0;
	private static Hashtable unsafe_connlist = Hashtable.Synchronized(new Hashtable());
	
	public static unsafe void M_InitConn(IntPtr *conn)
	{
		int id = Interlocked.Increment(ref unsafe_last_id);
		(*conn) = (IntPtr)id;
		
		M_CONN myconn = M_InitConn();
		unsafe_connlist[id] = myconn;
	}
	
	unsafe public static void M_DestroyConn(IntPtr* conn)
	{
		int id = (*conn).ToInt32();
		unsafe_connlist.Remove(id);
	}

	unsafe public static int M_SetIP(IntPtr* conn, string host, ushort port)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_SetIP(myconn, host, port)?1:0;
	}

	unsafe public static int M_SetSSL(IntPtr* conn, string host, ushort port)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_SetSSL(myconn, host, port)?1:0;
	}
	
	unsafe public static int M_SetDropFile(IntPtr *conn, string directory)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_SetDropFile(myconn, directory)?1:0;
	}
	
	unsafe public static int M_SetSSL_CAfile(IntPtr* conn, string path)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_SetSSL_CAfile(myconn, path)?1:0;
	}
	
	unsafe public static int M_SetSSL_Files(IntPtr* conn, string sslkeyfile, string sslcertfile)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_SetSSL_Files(myconn, sslkeyfile, sslcertfile)?1:0;
	}
	 
	unsafe public static void M_VerifySSLCert(IntPtr* conn, int tf)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		M_VerifySSLCert(myconn, tf != 0?true:false);
	}
	
	unsafe public static int M_SetBlocking(IntPtr *conn, int tf)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_SetBlocking(myconn, tf != 0?true:false)?1:0;
	}
	
	unsafe public static void M_MaxConnTimeout(IntPtr *conn, int maxtime)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		M_MaxConnTimeout(myconn, maxtime);
	}
	
	unsafe public static int M_SetTimeout(IntPtr *conn, int secs)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_SetTimeout(myconn, secs)?1:0;
	}
	
	unsafe public static int M_ValidateIdentifier(IntPtr *conn, int tf)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_ValidateIdentifier(myconn, tf != 0?true:false)?1:0;
	}
	
	unsafe public static void M_VerifyConnection(IntPtr *conn, int tf)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		M_VerifyConnection(myconn, tf != 0?true:false);
	}
	
	unsafe public static int M_Connect(IntPtr *conn)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_Connect(myconn)?1:0;
	}
	
	unsafe public static IntPtr M_TransNew(IntPtr *conn)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return (IntPtr)M_TransNew(myconn);
	}
	
	unsafe public static int M_TransKeyVal(IntPtr *conn, IntPtr id, string key, string val)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_TransKeyVal(myconn, id.ToInt32(), key, val)?1:0;
	}
	
	unsafe public static int M_TransSend(IntPtr *conn, IntPtr id)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_TransSend(myconn, id.ToInt32())?1:0;
	}
	
	unsafe public static int M_Monitor(IntPtr *conn)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_Monitor(myconn)?1:0;
	}
	
	unsafe public static int M_CheckStatus(IntPtr *conn, IntPtr id)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_CheckStatus(myconn, id.ToInt32());
	}
	
	unsafe public static int M_ReturnStatus(IntPtr *conn, IntPtr id)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_ReturnStatus(myconn, id.ToInt32());
	}
	
	unsafe public static void M_DeleteTrans(IntPtr *conn, IntPtr id)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		M_DeleteTrans(myconn, id.ToInt32());
	}

	unsafe public static string M_ConnectionError(IntPtr *conn)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_ConnectionError(myconn);
	}

	static int unsafe_last_keyid = 0;
	static Hashtable unsafe_keylist = Hashtable.Synchronized(new Hashtable());
	unsafe public static IntPtr M_ResponseKeys(IntPtr *conn, IntPtr id, int *num_keys)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		string[] keys = M_ResponseKeys(myconn, id.ToInt32());
		(*num_keys) = keys.Length;
		
		int keyid = Interlocked.Increment(ref unsafe_last_keyid);
		unsafe_keylist[keyid] = keys;
		return (IntPtr)keyid;
	}
	
	unsafe public static int M_FreeResponseKeys(IntPtr keys, int num_keys)
	{
		int id = keys.ToInt32();
		unsafe_keylist.Remove(id);
		return 1;
	}
	
	unsafe public static string M_ResponseKeys_index(IntPtr keys, int num_keys, int idx)
	{
		string[] mykeys = (string[])unsafe_keylist[keys.ToInt32()];
		return mykeys[idx];
	}

	unsafe public static int M_IsCommaDelimited(IntPtr *conn, IntPtr id)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_IsCommaDelimited(myconn, id.ToInt32())?1:0;
	}
	
	unsafe public static int M_ParseCommaDelimited(IntPtr *conn, IntPtr id)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_ParseCommaDelimited(myconn, id.ToInt32())?1:0;
	}
	
	unsafe public static int M_NumRows(IntPtr *conn, IntPtr id)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_NumRows(myconn, id.ToInt32());
	}
	
	unsafe public static int M_NumColumns(IntPtr *conn, IntPtr id)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_NumColumns(myconn, id.ToInt32());
	}
	
	unsafe public static string M_ResponseParam(IntPtr *conn, IntPtr id, string key)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_ResponseParam(myconn, id.ToInt32(), key);
	}

	unsafe public static string M_GetCell(IntPtr *conn, IntPtr id, string column, int row)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_GetCell(myconn, id.ToInt32(), column, row);
	}
	
	unsafe public static string M_GetCellByNum(IntPtr *conn, IntPtr id, int column, int row)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_GetCellByNum(myconn, id.ToInt32(), column, row);
	}
	
	unsafe public static string M_GetHeader(IntPtr *conn, IntPtr id, int column)
	{
		M_CONN myconn = (M_CONN)unsafe_connlist[(*conn).ToInt32()];
		return M_GetHeader(myconn, id.ToInt32(), column);
	}
	
	
	/* Unsafe implementation for VB.Net implementation which doesn't support
	 * an explicit by-ref &, but must be defined in the API exported */
	public static unsafe void M_InitConn(ref IntPtr conn)
	{
		IntPtr myconn;
		M_InitConn(&myconn);
		conn = myconn;
	}
	
	unsafe public static void M_DestroyConn(ref IntPtr conn)
	{
		IntPtr myconn = conn;
		M_DestroyConn(&myconn);
	}

	unsafe public static int M_SetIP(ref IntPtr conn, string host, ushort port)
	{
		IntPtr myconn = conn;
		return M_SetIP(&myconn, host, port);
	}

	unsafe public static int M_SetSSL(ref IntPtr conn, string host, ushort port)
	{
		IntPtr myconn = conn;
		return M_SetSSL(&myconn, host, port);
	}
	
	unsafe public static int M_SetDropFile(ref IntPtr conn, string directory)
	{
		IntPtr myconn = conn;
		return M_SetDropFile(&myconn, directory);
	}
	
	unsafe public static int M_SetSSL_CAfile(ref IntPtr conn, string path)
	{
		IntPtr myconn = conn;
		return M_SetSSL_CAfile(&myconn, path);
	}
	
	unsafe public static int M_SetSSL_Files(ref IntPtr conn, string sslkeyfile, string sslcertfile)
	{
		IntPtr myconn = conn;
		return M_SetSSL_Files(&myconn, sslkeyfile, sslcertfile);
	}
	 
	unsafe public static void M_VerifySSLCert(ref IntPtr conn, int tf)
	{
		IntPtr myconn = conn;
		M_VerifySSLCert(&myconn, tf);
	}
	
	unsafe public static int M_SetBlocking(ref IntPtr conn, int tf)
	{
		IntPtr myconn = conn;
		return M_SetBlocking(&myconn, tf);
	}
	
	unsafe public static void M_MaxConnTimeout(ref IntPtr conn, int maxtime)
	{
		IntPtr myconn = conn;
		M_MaxConnTimeout(&myconn, maxtime);
	}
	
	unsafe public static int M_SetTimeout(ref IntPtr conn, int secs)
	{
		IntPtr myconn = conn;
		return M_SetTimeout(&myconn, secs);
	}
	
	unsafe public static int M_ValidateIdentifier(ref IntPtr conn, int tf)
	{
		IntPtr myconn = conn;
		return M_ValidateIdentifier(&myconn, tf);
	}
	
	unsafe public static void M_VerifyConnection(ref IntPtr conn, int tf)
	{
		IntPtr myconn = conn;
		M_VerifyConnection(&myconn, tf);
	}
	
	unsafe public static int M_Connect(ref IntPtr conn)
	{
		IntPtr myconn = conn;
		return M_Connect(&myconn);
	}
	
	unsafe public static IntPtr M_TransNew(ref IntPtr conn)
	{
		IntPtr myconn = conn;
		return M_TransNew(&myconn);
	}
	
	unsafe public static int M_TransKeyVal(ref IntPtr conn, IntPtr id, string key, string val)
	{
		IntPtr myconn = conn;
		return M_TransKeyVal(&myconn, id, key, val);
	}
	
	unsafe public static int M_TransSend(ref IntPtr conn, IntPtr id)
	{
		IntPtr myconn = conn;
		return M_TransSend(&myconn, id);
	}
	
	unsafe public static int M_Monitor(ref IntPtr conn)
	{
		IntPtr myconn = conn;
		return M_Monitor(&myconn);
	}
	
	unsafe public static int M_CheckStatus(ref IntPtr conn, IntPtr id)
	{
		IntPtr myconn = conn;
		return M_CheckStatus(&myconn, id);
	}
	
	unsafe public static int M_ReturnStatus(ref IntPtr conn, IntPtr id)
	{
		IntPtr myconn = conn;
		return M_ReturnStatus(&myconn, id);
	}
	
	unsafe public static void M_DeleteTrans(ref IntPtr conn, IntPtr id)
	{
		IntPtr myconn = conn;
		M_DeleteTrans(&myconn, id);
	}

	unsafe public static string M_ConnectionError(ref IntPtr conn)
	{
		IntPtr myconn = conn;
		return M_ConnectionError(&myconn);
	}

	unsafe public static IntPtr M_ResponseKeys(ref IntPtr conn, IntPtr id, ref int num_keys)
	{
		int mykeys;
		IntPtr retval;
		IntPtr myconn = conn;
		retval = M_ResponseKeys(&myconn, id, &mykeys);
		num_keys = mykeys;
		return retval;
	}
	
	unsafe public static int M_IsCommaDelimited(ref IntPtr conn, IntPtr id)
	{
		IntPtr myconn = conn;
		return M_IsCommaDelimited(&myconn, id);
	}
	
	unsafe public static int M_ParseCommaDelimited(ref IntPtr conn, IntPtr id)
	{
		IntPtr myconn = conn;
		return M_ParseCommaDelimited(&myconn, id);
	}
	
	unsafe public static int M_NumRows(ref IntPtr conn, IntPtr id)
	{
		IntPtr myconn = conn;
		return M_NumRows(&myconn, id);
	}
	
	unsafe public static int M_NumColumns(ref IntPtr conn, IntPtr id)
	{
		IntPtr myconn = conn;
		return M_NumColumns(&myconn, id);
	}
	
	unsafe public static string M_ResponseParam(ref IntPtr conn, IntPtr id, string key)
	{
		IntPtr myconn = conn;
		return M_ResponseParam(&myconn, id, key);
	}

	unsafe public static string M_GetCell(ref IntPtr conn, IntPtr id, string column, int row)
	{
		IntPtr myconn = conn;
		return M_GetCell(&myconn, id, column, row);
	}
	
	unsafe public static string M_GetCellByNum(ref IntPtr conn, IntPtr id, int column, int row)
	{
		IntPtr myconn = conn;
		return M_GetCellByNum(&myconn, id, column, row);
	}
	
	unsafe public static string M_GetHeader(ref IntPtr conn, IntPtr id, int column)
	{
		IntPtr myconn = conn;
		return M_GetHeader(&myconn, id, column);
	}
#endif // USE_UNSAFE_API

};
	
};

