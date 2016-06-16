using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Credit Card Transaction Manager 2")]
[assembly: AssemblyDescription("IPS Parking Meter Credit Card Management System")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("IPS Group Inc")]
[assembly: AssemblyProduct("Credit Card Transaction Manager 2")] // to Clearing Platform - String 50
[assembly: AssemblyCopyright("Copyright © Private 2007")]
[assembly: AssemblyTrademark("IPS")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("71353ded-d017-4aec-8887-3f22ee49041d")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("3.0.17.0")]
[assembly: AssemblyFileVersion("3.0.17.0")]  // sent to Clearing Platform

// Release History (Most Recent on top)
// Note: Remember to update the following Version references.
//       i The two lines above
//      ii Project/Properties/Publish/Version
//
//  T0-Do's (List them here for to be included in future releases)
//  * Check / improve filtering of IPS Maintenance cards.
//  * Purging of aged 'New' track data.
//
//
//  -------------------------------------------------------------------------
//     V 1.0.0.12   25 October 2009, SA.
//     Released:    04 November 2009.
// 1. Incorporated ICVerify with First Data (FDMS Nashville) plug-in.
// 2. (10 Dec '09 CA) Added CCTransactionMngr1.Config2 to prevent a new compile 
//     overwriting custom configuration settings (previously stored in 
//     CCTransactionMngr1.exe.config
//
//  -------------------------------------------------------------------------
//     V 1.0.0.11   16 September 2009, SA.
//     Released:    16 September 2009.
// 1. Incorporated RsaUtils.RSADecrypt.
//    Changed Track2 length calc for each encryption type to suit.
// 2. Declined PANs are now save to the DB in Hashed format for blacklisting.
// 3. Declined tracks are now also purged (set to ‘Processed’).
//    
//  -------------------------------------------------------------------------
//     V 1.0.0.103   July 13 2009, SA.
//     Released July 13, 2009 
//   1. Changed target platform back from 'Any' to 'x86' (like previous versions)
//   2. Removed PCCOM.dll and PCCOM.ini from local folder
//
//  -------------------------------------------------------------------------
//     V 1.0.0.10   April, 2009 San Diego & June 2009, SA.
//     Released 09 July 2009 
//
//   1. Restructure code to provide for the addition of the MIGS (MasterCard
//      Internet Gateway Server) as a credit card processor to the Commonwealth
//      Bank for City of Perth in Australia. 
//   2. Implement Payment Client COM Library (PCCOMLib.dll).
//
//   3. For 'Special cards', store Card Type and PAN in TransactionRecord-
//      CreditCallCardScheme (varchar 50)
//
//   4. Changed exception handling to accommodate additions to code.
//
//
//  -------------------------------------------------------------------------
//     V 1.0.0.9   April 15, 2009 San Diego 
//   1. Incorporate code to identify and process test plainCCTracks. (This is 
//      required as a measure to re-process a number of initial dual space
//      records delivered as such.
//
//  -------------------------------------------------------------------------
//     V 1.0.0.8   April 2009 San Diego 
//   1. Evolve Transaction Record Status from 'New' to 'CCTM Processing' to
//      'Authorizing' and then to 'Approved' etc. to prevent an accidental 
//       duplicate resubmission of 'New' transactions by an accidental phantom  
//       2nd instance of CCTM. 
//
//   2. Save PAN in x.4 format on 'Approved' transactions. (ok)
//   3. Save PAN in #BL format on 'Declined' transactions. (ok)
//   3. Delete CCTracks on all processed transactions.
// 
//   4. Move updateRecordStatus to after updateRecordDetails to prevent 
//      Record Status update to 'Declined' while CreditCallPAN = NULL, 
//      as this can result in a NULL being loaded into the BL.
//
//   5. Set Transaction Submission Interval Default to 20s.
//
//   6. tmp disable conversion of plaintext PAN on Declined Transactions
//      to #BL format (see 2. & 3. above)to allow meters to download 
//      #BL compatible firmware first.
//
//
//  -------------------------------------------------------------------------
//     V 1.0.0.7 26 November 2008 - Tested and ready for release
//  1. 19 November 2008
//   i  Corrupt track passed crude validity check ("=" found in data), causing 
//     'Bad Request' exception not trapped, resulting in Suspended transaction
//      being perpetually re-submitted - Fixed.
//  -------------------------------------------------------------------------
//     V 1.0.0.6 (Released on 18 November 2008)
//  1. 18 November 2008
//   i  Nest V1.0.0.5/3.i inside crude validity check to prevent system exception
//      on search for the '=' index on corrupt track data. This restores the correct
//      functionality of 'Invalid Track Data' handling.
//
//  -------------------------------------------------------------------------
//     V 1.0.0.5 (Released on 21 October 2008)
//  3. 19 October 2008 
//   i Blacklist candidate identifier. Save full PAN in CC Transaction Table 
//     for Declined transaction. SQL app. compiles blacklist from these entries.
//  
//  2. July 2007 
//   i Included Track1 extraction to improve mag head reliability, but 
//     commented this out for possible future use as this is optional
//     whereas Track2 is mandatory.
//
//  1. 28 April 2007 
//   i  Create CardEase exception handler for " ... unable to connect to 
//      remote server". Leave CCTransactionStatus as 'New' to enable re-
//      try on next submission cycle. 
//   ii Simplified 'Try-Catch' structures in
//      the process.
//  -------------------------------------------------------------------------
//     V 1.0.0.4 - 11 April 2007
//  2. Increase size of Event Log window.
//
//  1. Update CcTransactionStatus to 'Suspended' when CardEase server throws
//     a SystemException (400) 'Bad Request' due to say corrupt track data.
//

