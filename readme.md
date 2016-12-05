# Transaction Management

Transaction Management is responsible for card payment authorization from the
meter and ancilliary services.

Please refer to https://innovate.ipsgroupinc.com/display/SH/Repositories for
information relating to development environment, build instructions, and
deployment instructions.

Please refer to https://innovate.ipsgroupinc.com/display/SH/Session+and+Transaction+Management
for the main portal for Transaction Management.


## Components 

The main components of the software suite are:

1. RTCC - Real Time Credit Card processor.
   Responsible for handling real-time credit card payment authorization requests 
   coming from a meter.
2. CCTM - Credit Card Transaction Manager.
   Responsible for handling any offline credit card payments that were locally 
   authorized / stood-in by a meter.
3. Refunder
   Responsible for processing transactions marked for refunds.
   NOTE: Only for customers utilizing Monetra as the payment switch.


## Visual Studio Solution

The parent solution for all projects is TransactionManagement.sln.

