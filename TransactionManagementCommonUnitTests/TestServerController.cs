using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransactionManagementCommon;
using TransactionManagementCommon.ControllerBase;

namespace UnitTests
{
    [TestClass]
    public class TestServerController
    {
        [TestMethod]
        public void TestServerController_BasicOperation()
        {
            ServerFactory<MyServer> myServerFactory = new SimpleServerFactory<MyServer>(()=>new MyServer());
            ServerController<MyServer> myServer = new ServerController<MyServer>(myServerFactory);
            int added = myServer.Perform<int>((server)=>server.DummyOperation(1, 2));
            Assert.AreEqual(3, added);
        }

        [TestMethod]
        public void TestServerController_OperationFailBehavior()
        {
            ServerFactory<MyServer> myServerFactory = new SimpleServerFactory<MyServer>(() => new MyServer());
            ServerController<MyServer> myServer = new ServerController<MyServer>(myServerFactory);
            try
            {
                myServer.Perform(
                    (server) => server.DummyFailureOperation(),
                    (exception, triedCount) => triedCount < 5 ? OperationFailAction.RetryNoRestart : OperationFailAction.Abort
                    );
            }
            catch
            { 
            }
            Assert.AreEqual(MyServer.FailOpCount, 5);
        }

        [TestMethod]
        public void TestServerController_OperationRetryBehavior()
        {
            ServerFactory<MyServer> myServerFactory = new SimpleServerFactory<MyServer>(() => new MyServer());
            ServerController<MyServer> myServer = new ServerController<MyServer>(myServerFactory);
            myServer.Perform(
                (server) => server.DummyFailureOperation(),
                (exception, triedCount) => OperationFailAction.RetryNoRestart
                );
            Assert.AreEqual(MyServer.FailOpCount, 10);
        }

        [TestMethod]
        public void TestServerController_MultiRestart()
        {
            ServerFactory<MyServer> myServerFactory = new SimpleServerFactory<MyServer>(() => new MyServer());
            ServerController<MyServer> myServer = new ServerController<MyServer>(myServerFactory);

            // Try an operation - this should work
            int result = myServer.Perform<int>((server) => server.DummyOperation(1, 2));
            Assert.AreEqual(3, result);

            // Tell the server to fail next time it restarts
            MyServer.FailConstructor = true;

            // When the server fails to restart, it must try again
            myServer.FailedRestart +=
                (exception, retryCount) =>
                {
                    MyServer.FailConstructor = false;
                    return RestartFailAction.Retry;
                };

            Assert.AreEqual(1, MyServer.StartCount);

            // Put the current server into a "bad" state where it needs a restart
            myServer.Set<bool>((server, value) => server.BadState = value, true);

            // Do an operation that will force a restart
            result = myServer.Perform<int>(
                operation: (server) => server.DummyOperation(2, 3),
                exceptionHandler: (exception, retryCount) =>
                    OperationFailAction.RestartAndRetry
                );

            // Check that it restarted twice - the first time the restart would have failed, the second time it would have worked
            Assert.AreEqual(3, MyServer.StartCount);

            // Check that it got the result
            Assert.AreEqual(5, result);

            // Try an operation - this should work
            result = myServer.Perform<int>((server) => server.DummyOperation(5, 3));
            Assert.AreEqual(8, result);
        }
    }

    public class MyServer
    {
        public static int FailOpCount = 0;
        public static bool FailConstructor = false;
        public static int StartCount = 0;
        public bool BadState = false;

        public MyServer()
        {
            StartCount++;
            if (FailConstructor)
                throw new Exception("Dummy failure");
        }

        public int DummyOperation(int num1, int num2)
        {
            if (BadState)
                throw new Exception("Dummy failure");
            return num1 + num2;
        }

        public void DummyFailureOperation()
        {
            FailOpCount++;
            if (FailOpCount < 10)
                throw new Exception("Dummy failure");
        }
    }
}

