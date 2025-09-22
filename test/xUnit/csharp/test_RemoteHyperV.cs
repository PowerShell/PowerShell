// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Management.Automation.Subsystem;
using System.Management.Automation.Subsystem.Prediction;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PSTests.Sequential
{
    public class RemoteHyperVTests
    {
        private static ITestOutputHelper _output;
        private static TimeSpan timeout = TimeSpan.FromSeconds(15);

        public RemoteHyperVTests(ITestOutputHelper output)
        {
            if (!System.Management.Automation.Platform.IsWindows)
            {
                throw new SkipException("RemoteHyperVTests are only supported on Windows.");
            }

            _output = output;
        }

        // Helper method to connect with retries
        private static void ConnectWithRetry(Socket client, IPAddress address, int port, ITestOutputHelper output, int maxRetries = 10)
        {
            int retryDelayMs = 500;
            int attempt = 0;
            bool connected = false;
            while (attempt < maxRetries && !connected)
            {
                try
                {
                    client.Connect(address, port);
                    connected = true;
                }
                catch (SocketException)
                {
                    attempt++;
                    if (attempt < maxRetries)
                    {
                        output?.WriteLine($"Connect attempt {attempt} failed, retrying in {retryDelayMs}ms...");
                        Thread.Sleep(retryDelayMs);
                        retryDelayMs *= 2;
                    }
                    else
                    {
                        output?.WriteLine($"Failed to connect after {maxRetries} attempts.  This is most likely an intermittent failure due to environmental issues.");
                        throw;
                    }
                }
            }
        }

        private static void SendResponse(string name, Socket client, Queue<(byte[] Bytes, int DelayMs)> serverResponses)
        {
            if (serverResponses.Count > 0)
            {
                _output.WriteLine($"Mock {name} ----------------------------------------------------");
                var respTuple = serverResponses.Dequeue();
                var resp = respTuple.Bytes;

                if (respTuple.DelayMs > 0)
                {
                    _output.WriteLine($"Mock {name} - delaying response by {respTuple.DelayMs} ms");
                    Thread.Sleep(respTuple.DelayMs);
                }
                if (resp.Length > 0) {
                    client.Send(resp, resp.Length, SocketFlags.None);
                    _output.WriteLine($"Mock {name} - sent response: " + Encoding.ASCII.GetString(resp));
                }
            }
        }

        private static void StartHandshakeServer(
            string name,
            int port,
            IEnumerable<(string Message, Encoding Encoding)> expectedClientSends,
            IEnumerable<(string Message, Encoding Encoding)> serverResponses,
            bool verifyConnectionClosed,
            CancellationToken cancellationToken,
            bool sendFirst = false)
        {
            IEnumerable<(string Message, Encoding Encoding, int DelayMs)> serverResponsesWithDelay = new List<(string Message, Encoding Encoding, int DelayMs)>();
            foreach (var item in serverResponses)
            {
                ((List<(string Message, Encoding Encoding, int DelayMs)>)serverResponsesWithDelay).Add((item.Message, item.Encoding, 1));
            }
            StartHandshakeServer(name, port, expectedClientSends, serverResponsesWithDelay, verifyConnectionClosed, cancellationToken, sendFirst);
        }

        private static void StartHandshakeServer(
            string name,
            int port,
            IEnumerable<(string Message, Encoding Encoding)> expectedClientSends,
            IEnumerable<(string Message, Encoding Encoding, int DelayMs)> serverResponses,
            bool verifyConnectionClosed,
            CancellationToken cancellationToken,
            bool sendFirst = false)
        {
            var expectedMessages = new Queue<(string Message, byte[] Bytes, Encoding Encoding)>();
            foreach (var item in expectedClientSends)
            {
                var itemBytes = item.Encoding.GetBytes(item.Message);
                expectedMessages.Enqueue((Message: item.Message, Bytes: itemBytes, Encoding: item.Encoding));
            }

            var serverResponseBytes = new Queue<(byte[] Bytes, int DelayMs)>();
            foreach (var item in serverResponses)
            {
                (byte[] Bytes, int DelayMs) queueItem = (item.Encoding.GetBytes(item.Message), item.DelayMs);
                serverResponseBytes.Enqueue(queueItem);
            }

            _output.WriteLine($"Mock {name} - starting listener on port {port} with {expectedMessages.Count} expected messages and {serverResponseBytes.Count} responses.");
            StartHandshakeServerImplementation(name, port, expectedMessages, serverResponseBytes, verifyConnectionClosed, cancellationToken, sendFirst);
        }

        private static void StartHandshakeServerImplementation(
            string name,
            int port,
            Queue<(string Message, byte[] Bytes, Encoding Encoding)> expectedClientSends,
            Queue<(byte[] Bytes, int DelayMs)> serverResponses,
            bool verifyConnectionClosed,
            CancellationToken cancellationToken,
            bool sendFirst = false)
        {
            DateTime startTime = DateTime.UtcNow;
            var buffer = new byte[1024];
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            try
            {
                using (var client = listener.AcceptSocket())
                {
                    if (sendFirst)
                    {
                        // Send the first message from the serverResponses queue
                        SendResponse(name, client, serverResponses);
                    }

                    while (expectedClientSends.Count > 0)
                    {
                        _output.WriteLine($"Mock {name} - time elapsed: {(DateTime.UtcNow - startTime).TotalMilliseconds} milliseconds");
                        client.ReceiveTimeout = 2 * 1000; // 2 seconds timeout for receiving data
                        cancellationToken.ThrowIfCancellationRequested();
                        var expectedMessage = expectedClientSends.Dequeue();
                        _output.WriteLine($"Mock {name} - remaining expected messages: {expectedClientSends.Count}");
                        var expected = expectedMessage.Bytes;
                        Array.Clear(buffer, 0, buffer.Length);
                        int received = client.Receive(buffer);
                        // Optionally validate received data matches expected
                        string expectedString = expectedMessage.Message;
                        string bufferString = expectedMessage.Encoding.GetString(buffer, 0, received);
                        string alternativeEncodedString = string.Empty;
                        if (expectedMessage.Encoding == Encoding.Unicode)
                        {
                            alternativeEncodedString = Encoding.UTF8.GetString(buffer, 0, received);
                        }
                        else if (expectedMessage.Encoding == Encoding.UTF8)
                        {
                            alternativeEncodedString = Encoding.Unicode.GetString(buffer, 0, received);
                        }

                        if (received != expected.Length)
                        {
                            string errorMessage = $"Mock {name} - Expected {expected.Length} bytes, but received {received} bytes: `{bufferString}`(alt encoding: `{alternativeEncodedString}`); expected: {expectedString}";
                            _output.WriteLine(errorMessage);
                            throw new Exception(errorMessage);
                        }
                        if (!string.Equals(bufferString, expectedString, StringComparison.OrdinalIgnoreCase))
                        {
                            string errorMessage = $"Mock {name} - Expected `{expectedString}`; length {expected.Length}, but received; length {received}; `{bufferString}`(alt encoding: `{alternativeEncodedString}`) instead.";
                            _output.WriteLine(errorMessage);
                            throw new Exception(errorMessage);
                        }
                        _output.WriteLine($"Mock {name} - received expected message: " + expectedString);
                        SendResponse(name, client, serverResponses);
                    }

                    if (verifyConnectionClosed)
                    {
                        _output.WriteLine($"Mock {name} - verifying client connection is closed.");
                        // Wait for the client to close the connection synchronously (no timeout)
                        try
                        {
                            while (true)
                            {
                                int bytesRead = client.Receive(buffer, SocketFlags.None);
                                if (bytesRead == 0)
                                {
                                    break;
                                }

                                // If we receive any data, log and throw (assume UTF8 encoding)
                                string unexpectedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                _output.WriteLine($"Mock {name} - received unexpected data after handshake: {unexpectedData}");
                                throw new Exception($"Mock {name} - received unexpected data after handshake: {unexpectedData}");
                            }
                            _output.WriteLine($"Mock {name} - client closed the connection.");
                        }
                        catch (SocketException ex)
                        {
                            _output.WriteLine($"Mock {name} - socket exception while waiting for client close: {ex.Message} {ex.GetType().FullName}");
                        }
                        catch (ObjectDisposedException)
                        {
                            _output.WriteLine($"Mock {name} - socket already closed.");
                            // Socket already closed
                        }
                    }
                }

                _output.WriteLine($"Mock {name} - on port {port} completed successfully.");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Mock {name} - Exception: {ex.Message} {ex.GetType().FullName}");
                _output.WriteLine(ex.StackTrace);
                throw;
            }
            finally
            {
                _output.WriteLine($"Mock {name} - remaining expected messages: {expectedClientSends.Count}");
                _output.WriteLine($"Mock {name} - stopping listener on port {port}.");
                listener.Stop();
            }
        }

        // Helper function to create a random 4-character ASCII response
        private static string CreateRandomAsciiResponse()
        {
            var rand = new Random();
            // Randomly return either "PASS" or "FAIL"
            return rand.Next(0, 2) == 0 ? "PASS" : "FAIL";
        }

        // Helper method to create test data
        private static (List<(string Message, Encoding Encoding)> expectedClientSends, List<(string Message, Encoding Encoding)> serverResponses) CreateHandshakeTestData(NetworkCredential cred)
        {
            var expectedClientSends = new List<(string Message, Encoding Encoding)>
            {
                (Message: cred.Domain, Encoding: Encoding.Unicode),
                (Message: cred.UserName, Encoding: Encoding.Unicode),
                (Message: "NONEMPTYPW", Encoding: Encoding.ASCII),
                (Message: cred.Password, Encoding: Encoding.Unicode)
            };

            var serverResponses = new List<(string Message, Encoding Encoding)>
            {
                (Message: CreateRandomAsciiResponse(), Encoding: Encoding.ASCII), // Response to domain
                (Message: CreateRandomAsciiResponse(), Encoding: Encoding.ASCII), // Response to username
                (Message: CreateRandomAsciiResponse(), Encoding: Encoding.ASCII)  // Response to non-empty password
            };

            return (expectedClientSends, serverResponses);
        }

        private static List<(string Message, Encoding Encoding)> CreateVersionNegotiationClientSends()
        {
            return new List<(string Message, Encoding Encoding)>
            {
                (Message: "VERSION", Encoding: Encoding.UTF8),
                (Message: "VERSION_2", Encoding: Encoding.UTF8),
            };
        }

        private static List<(string Message, Encoding Encoding)> CreateV2Sends(NetworkCredential cred, string configurationName)
        {
            var sends = CreateVersionNegotiationClientSends();
            var password = cred.Password;
            var emptyPassword = string.IsNullOrEmpty(password);

            sends.AddRange(new List<(string Message, Encoding Encoding)>
            {
                (Message: cred.Domain, Encoding: Encoding.Unicode),
                (Message: cred.UserName, Encoding: Encoding.Unicode)
            });

            if (!emptyPassword)
            {
                sends.AddRange(new List<(string Message, Encoding Encoding)>
                {
                    (Message: "NONEMPTYPW", Encoding: Encoding.UTF8),
                    (Message: cred.Password, Encoding: Encoding.Unicode)
                });
            }
            else
            {
                sends.Add((Message: "EMPTYPW", Encoding: Encoding.UTF8)); // Empty password and we don't expect a response
            }

            if (!string.IsNullOrEmpty(configurationName))
            {
                sends.Add((Message: "NONEMPTYCF", Encoding: Encoding.UTF8));
                sends.Add((Message: configurationName, Encoding: Encoding.Unicode)); // Configuration string and we don't expect a response
            }
            else
            {
                sends.Add((Message: "EMPTYCF", Encoding: Encoding.UTF8)); // Configuration string and we don't expect a response
            }

            sends.Add((Message: "PASS", Encoding: Encoding.ASCII)); // Response to TOKEN

            return sends;
        }

        private static List<(string Message, Encoding Encoding)> CreateV2Responses(string version = "VERSION_2", bool emptyConfig = false, string token = "FakeToken0+/=", bool emptyPassword = false)
        {
            var responses = new List<(string Message, Encoding Encoding)>
            {
                (Message: version, Encoding: Encoding.ASCII), // Response to VERSION
                (Message: "PASS", Encoding: Encoding.ASCII), // Response to VERSION_2
                (Message: "PASS", Encoding: Encoding.ASCII), // Response to domain
                (Message: "PASS", Encoding: Encoding.ASCII), // Response to username
            };

            if (!emptyPassword)
            {
                responses.Add((Message: "PASS", Encoding: Encoding.ASCII));  // Response to non-empty password
            }

            responses.Add((Message: "CONF", Encoding: Encoding.ASCII)); // Response to configuration

            if (!emptyConfig)
            {
                responses.Add((Message: "PASS", Encoding: Encoding.ASCII));  // Response to non-empty configuration
            }
            responses.Add((Message: "TOKEN " + token, Encoding: Encoding.ASCII)); // Response to with a token than uses each class of character in base 64 encoding

            return responses;
        }

        // Helper method to create test data
        private static (List<(string Message, Encoding Encoding)> expectedClientSends, List<(string Message, Encoding Encoding)> serverResponses)
                CreateHandshakeTestDataV2(NetworkCredential cred, string version, string configurationName, string token)
        {
            bool emptyConfig = string.IsNullOrEmpty(configurationName);
            bool emptyPassword = string.IsNullOrEmpty(cred.Password);
            return (CreateV2Sends(cred, configurationName), CreateV2Responses(version, emptyConfig, token, emptyPassword));
        }

        // Helper method to create test data
        private static (List<(string Message, Encoding Encoding)> expectedClientSends, List<(string Message, Encoding Encoding)> serverResponses) CreateHandshakeTestDataForFallback(NetworkCredential cred)
        {
            var expectedClientSends = new List<(string Message, Encoding Encoding)>
            {
                (Message: "VERSION", Encoding: Encoding.UTF8),
                (Message: @"?<PSDirectVMLegacy>", Encoding: Encoding.Unicode),
                (Message: "EMPTYPW", Encoding: Encoding.UTF8), // Response to domain
                (Message: "FAIL", Encoding: Encoding.UTF8), // Response to domain
            };

            List<(string Message, Encoding Encoding)> serverResponses = new List<(string Message, Encoding Encoding)>
            {
                (Message: "PASS", Encoding: Encoding.ASCII), // Response to VERSION but v1 server expects domain so it says "PASS"
                (Message: "PASS", Encoding: Encoding.ASCII), // Response to username
                (Message: "FAIL", Encoding: Encoding.ASCII) // Response to EMPTYPW
            };

            return (expectedClientSends, serverResponses);
        }

        // Helper to create a password with at least one non-ASCII Unicode character
        public static string CreateRandomUnicodePassword(string prefix)
        {
            var rand = new Random();
            var asciiPart = new char[6 + prefix.Length];
            // Copy prefix into asciiPart
            Array.Copy(prefix.ToCharArray(), 0, asciiPart, 0, prefix.Length);
            for (int i = prefix.Length; i < asciiPart.Length; i++)
            {
                asciiPart[i] = (char)rand.Next(33, 127); // ASCII printable
            }
            // Add a random Unicode character outside ASCII range (e.g., U+0100 to U+017F)
            char unicodeChar = (char)rand.Next(0x0100, 0x017F);
            // Insert the unicode character at a random position
            int insertPos = rand.Next(0, asciiPart.Length + 1);
            var passwordChars = new List<char>(asciiPart);
            passwordChars.Insert(insertPos, unicodeChar);
            return new string(passwordChars.ToArray());
        }

        public static NetworkCredential CreateTestCredential()
        {
            return new NetworkCredential(CreateRandomUnicodePassword("username"), CreateRandomUnicodePassword("password"), CreateRandomUnicodePassword("domain"));
        }

        [SkippableFact]
        public async Task PerformCredentialAndConfigurationHandshake_V1_Pass()
        {
            // Arrange
            int port = 50000 + (int)(DateTime.Now.Ticks % 10000);
            var cred = CreateTestCredential();
            string configurationName = CreateRandomUnicodePassword("config");

            var (expectedClientSends, serverResponses) = CreateHandshakeTestData(cred);
            expectedClientSends.Add(("PASS", Encoding.ASCII));
            serverResponses.Add(("PASS", Encoding.ASCII));

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var serverTask = Task.Run(() => StartHandshakeServer("Broker", port, expectedClientSends, serverResponses, verifyConnectionClosed: false, cts.Token), cts.Token);

            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                ConnectWithRetry(client, IPAddress.Loopback, port, _output);
                var exchangeResult = System.Management.Automation.Remoting.RemoteSessionHyperVSocketClient.ExchangeCredentialsAndConfiguration(cred, configurationName, client, true);
                var result = exchangeResult.success;
                _output.WriteLine($"Exchange result: {result}, Token: {exchangeResult.authenticationToken}");
                System.Threading.Thread.Sleep(100); // Allow time for server to process
                Assert.True(result, $"Expected Exchange to pass");
            }

            await serverTask;
        }

        [SkippableTheory]
        [InlineData("VERSION_2", "configurationname1", "FakeTokenaaaaaaaaaAAAAAAAAAAAAAAAAAAAAAA0FakeTokenaaaaaaaaaAAAAAAAAAAAAAAAAAAAAAA0+/==")] // a fake base64 token about 512 bits long (double the size when this was spec'ed)
        [InlineData("VERSION_10", null, "FakeTokenaaaaaaaaaAAAAAAAAAAAAAAAAAAAAAA0+/=")] // a fake base64 token about 256 bits Long (the size when this was spec'ed)
        public async Task PerformCredentialAndConfigurationHandshake_V2_Pass(string versionResponse, string configurationName, string token)
        {
            // Arrange
            int port = 50000 + (int)(DateTime.Now.Ticks % 10000);
            var cred = CreateTestCredential();

            var (expectedClientSends, serverResponses) = CreateHandshakeTestDataV2(cred, versionResponse, configurationName, token);

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var serverTask = Task.Run(() => StartHandshakeServer("Broker", port, expectedClientSends, serverResponses, verifyConnectionClosed: true, cts.Token), cts.Token);

            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                client.Connect(IPAddress.Loopback, port);
                var exchangeResult = System.Management.Automation.Remoting.RemoteSessionHyperVSocketClient.ExchangeCredentialsAndConfiguration(cred, configurationName, client, false);
                var result = exchangeResult.success;
                System.Threading.Thread.Sleep(100); // Allow time for server to process
                Assert.True(result, $"Expected Exchange to pass for version response '{versionResponse}'");
                Assert.Equal(token, exchangeResult.authenticationToken);
            }

            await serverTask;
        }

        [SkippableFact]
        public async Task PerformCredentialAndConfigurationHandshake_V1_Fallback()
        {
            // Arrange
            int port = 50000 + (int)(DateTime.Now.Ticks % 10000);
            var cred = CreateTestCredential();
            string configurationName = CreateRandomUnicodePassword("config");

            var (expectedClientSends, serverResponses) = CreateHandshakeTestDataForFallback(cred);

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var serverTask = Task.Run(() => StartHandshakeServer("Broker", port, expectedClientSends, serverResponses, verifyConnectionClosed: false, cts.Token), cts.Token);

            bool isFallback = false;
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                _output.WriteLine("Starting handshake with V2 protocol.");
                client.Connect(IPAddress.Loopback, port);
                var exchangeResult = System.Management.Automation.Remoting.RemoteSessionHyperVSocketClient.ExchangeCredentialsAndConfiguration(cred, configurationName, client, false);
                isFallback = !exchangeResult.success;

                System.Threading.Thread.Sleep(100); // Allow time for server to process
                _output.WriteLine("Handshake indicated fallback to V1.");
                Assert.True(isFallback, "Expected fallback to V1.");
            }
            _output.WriteLine("Handshake completed successfully with fallback to V1.");

            await serverTask;
        }

        [SkippableFact]
        public async Task PerformCredentialAndConfigurationHandshake_V2_InvalidResponse()
        {
            // Arrange
            int port = 51000 + (int)(DateTime.Now.Ticks % 10000);
            var cred = CreateTestCredential();

            var (expectedClientSends, serverResponses) = CreateHandshakeTestData(cred);
            //expectedClientSends.Add("FAI1");
            serverResponses.Add(("FAI1", Encoding.ASCII));

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            //cts.Token.Register(() => throw new OperationCanceledException("Test timed out."));

            var serverTask = Task.Run(() => StartHandshakeServer("Broker", port, expectedClientSends, serverResponses, verifyConnectionClosed: false, cts.Token), cts.Token);

            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                _output.WriteLine("connecting on port " + port);
                ConnectWithRetry(client, IPAddress.Loopback, port, _output);

                var ex = Record.Exception(() => System.Management.Automation.Remoting.RemoteSessionHyperVSocketClient.ExchangeCredentialsAndConfiguration(cred, "config", client, true));

                try
                {
                    await serverTask;
                }
                catch (AggregateException exAgg)
                {
                    Assert.Null(exAgg.Flatten().InnerExceptions[1].Message);
                }
                cts.Token.ThrowIfCancellationRequested();

                Assert.NotNull(ex);
                Assert.NotNull(ex.Message);
                Assert.Contains("Hyper-V Broker sent an invalid Credential response", ex.Message);
            }
        }

        [SkippableFact]
        public async Task PerformCredentialAndConfigurationHandshake_V1_Fail()
        {
            // Arrange
            int port = 51000 + (int)(DateTime.Now.Ticks % 10000);
            var cred = CreateTestCredential();

            var (expectedClientSends, serverResponses) = CreateHandshakeTestData(cred);
            expectedClientSends.Add(("FAIL", Encoding.ASCII));
            serverResponses.Add(("FAIL", Encoding.ASCII));

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            // This scenario does not close the connection in a timely manner, so we set verifyConnectionClosed to false
            var serverTask = Task.Run(() => StartHandshakeServer("Broker", port, expectedClientSends, serverResponses, verifyConnectionClosed: false, cts.Token), cts.Token);

            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                client.Connect(IPAddress.Loopback, port);

                var ex = Record.Exception(() => System.Management.Automation.Remoting.RemoteSessionHyperVSocketClient.ExchangeCredentialsAndConfiguration(cred, "config", client, true));

                try
                {
                    await serverTask;
                }
                catch (AggregateException exAgg)
                {
                    Assert.Null(exAgg.Flatten().InnerExceptions[1].Message);
                }

                cts.Token.ThrowIfCancellationRequested();

                Assert.NotNull(ex);
                Assert.NotNull(ex.Message);
                Assert.Contains("The credential is invalid.", ex.Message);
            }
        }

        [SkippableTheory]
        [InlineData("VERSION_2", "FakeTokenaaaaaaaaaAAAAAAAAAAAAAAAAAAAAAA0FakeTokenaaaaaaaaaAAAAAAAAAAAAAAAAAAAAAA0+/==")] // a fake base64 token about 512 bits long (double the size when this was spec'ed)
        [InlineData("VERSION_10", "FakeTokenaaaaaaaaaAAAAAAAAAAAAAAAAAAAAAA0+/=")] // a fake base64 token about 256 bits Long (the size when this was spec'ed)
        public async Task PerformTransportVersionAndTokenExchange_Pass(string version, string token)
        {
            // Arrange
            int port = 50000 + (int)(DateTime.Now.Ticks % 10000);
            var cred = CreateTestCredential();

            var expectedClientSends = CreateVersionNegotiationClientSends();
            expectedClientSends.Add((Message: "TOKEN " + token, Encoding: Encoding.ASCII));

            var serverResponses = new List<(string Message, Encoding Encoding)>{
                (Message: version, Encoding: Encoding.ASCII), // Response to VERSION
                (Message: "PASS", Encoding: Encoding.ASCII), // Response to VERSION_2
                (Message: "PASS", Encoding: Encoding.ASCII) // Response to token
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var serverTask = Task.Run(() => StartHandshakeServer("Server", port, expectedClientSends, serverResponses, verifyConnectionClosed: true, cts.Token), cts.Token);

            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                ConnectWithRetry(client, IPAddress.Loopback, port, _output);
                System.Management.Automation.Remoting.RemoteSessionHyperVSocketClient.PerformTransportVersionAndTokenExchange(client, token);
                System.Threading.Thread.Sleep(100); // Allow time for server to process
            }

            await serverTask;
        }

        [SkippableTheory]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(0, false)]
        [InlineData(null, false)]
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public void IsRequirePsDirectAuthenticationEnabled(int? regValue, bool expected)
        {
            const string testKeyPath = @"SOFTWARE\Microsoft\TestRequirePsDirectAuthentication";
            const string valueName = "RequirePsDirectAuthentication";
            if (!System.Management.Automation.Platform.IsWindows)
            {
                throw new SkipException("RemoteHyperVTests are only supported on Windows.");
            }

            // Clean up any previous test key
            var regHive = Microsoft.Win32.RegistryHive.CurrentUser;
            var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(regHive, Microsoft.Win32.RegistryView.Registry64);
            baseKey.DeleteSubKeyTree(testKeyPath, false);

            bool? result = null;

            // Create the test key
            using (var key = baseKey.CreateSubKey(testKeyPath))
            {
                if (regValue.HasValue)
                {
                    key.SetValue(valueName, regValue.Value, Microsoft.Win32.RegistryValueKind.DWord);
                }
                else
                {
                    // Ensure the value does not exist
                    key.DeleteValue(valueName, false);
                }

                result = System.Management.Automation.Remoting.RemoteSessionHyperVSocketClient.IsRequirePsDirectAuthenticationEnabled(testKeyPath, regHive);
            }

            Assert.True(result.HasValue, "IsRequirePsDirectAuthenticationEnabled should return a value.");
            Assert.True(expected == result.Value,
                $"Expected IsRequirePsDirectAuthenticationEnabled to return {expected} when registry value is {(regValue.HasValue ? regValue.ToString() : "not set")}.");

            return;
        }

        [SkippableTheory]
        [InlineData("testToken", "testToken")]
        [InlineData("testToken\0", "testToken")]
        public async Task ValidatePassesWhenTokensMatch(string token, string expectedToken)
        {
            int port = 50000 + (int)(DateTime.Now.Ticks % 10000);

            var expectedClientSends = new List<(string Message, Encoding Encoding)>
            {
                (Message: "VERSION", Encoding: Encoding.ASCII), // Response to VERSION
                (Message: "VERSION_2", Encoding: Encoding.ASCII), // Response to VERSION_2
                (Message: $"TOKEN {token}", Encoding: Encoding.ASCII)
            };

            var serverResponses = new List<(string Message, Encoding Encoding)>
            {
                (Message: "VERSION_2", Encoding: Encoding.ASCII), // Response to VERSION_2
                (Message: "PASS", Encoding: Encoding.ASCII), // Response to VERSION_2
                (Message: "PASS", Encoding: Encoding.ASCII) // Response to token
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var serverTask = Task.Run(() => StartHandshakeServer("Client", port, serverResponses, expectedClientSends, verifyConnectionClosed: true, cts.Token, sendFirst: true), cts.Token);

            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                ConnectWithRetry(client, IPAddress.Loopback, port, _output);
                System.Management.Automation.Remoting.RemoteSessionHyperVSocketServer.ValidateToken(client, expectedToken, DateTimeOffset.UtcNow, 1);
                System.Threading.Thread.Sleep(100); // Allow time for server to process
            }

            await serverTask;
        }

        [SkippableTheory]
        [InlineData(5500, "A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond.", "SocketException")] // test the socket timeout
        [InlineData(3200, "canceled", "System.OperationCanceledException")] // test the cancellation token
        [InlineData(10, "", "")]
        public async Task ValidateTokenTimeoutFails(int timeoutMs, string expectedMessage, string expectedExceptionType = "SocketException")
        {
            string token = "testToken";
            string expectedToken = token;
            int port = 50000 + (int)(DateTime.Now.Ticks % 10000);

            var expectedClientSends = new List<(string Message, Encoding Encoding, int DelayMs)>
            {
                (Message: "VERSION", Encoding: Encoding.ASCII, DelayMs: timeoutMs), // Response to VERSION
                (Message: "VERSION_2", Encoding: Encoding.ASCII, DelayMs: timeoutMs), // Response to VERSION_2
                (Message: $"TOKEN {token}", Encoding: Encoding.ASCII, DelayMs: 1),
            };

            var serverResponses = new List<(string Message, Encoding Encoding)>
            {
                (Message: "VERSION_2", Encoding: Encoding.ASCII), // Response to VERSION_2
                (Message: "PASS", Encoding: Encoding.ASCII), // Response to VERSION_2
                (Message: "PASS", Encoding: Encoding.ASCII) // Response to token
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var serverTask = Task.Run(() => StartHandshakeServer("Client", port, serverResponses, expectedClientSends, verifyConnectionClosed: true, cts.Token, sendFirst: true), cts.Token);

            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                ConnectWithRetry(client, IPAddress.Loopback, port, _output);
                if (expectedMessage.Length > 0)
                {
                    var exception = Record.Exception(
                        () => System.Management.Automation.Remoting.RemoteSessionHyperVSocketServer.ValidateToken(client, expectedToken, DateTimeOffset.UtcNow, 5)); // set the timeout to  5 seconds or 5000 ms
                    Assert.NotNull(exception);
                    string exceptionType = exception.GetType().FullName;
                    _output.WriteLine($"Caught exception of type {exceptionType} with message: {exception.Message}");
                    Assert.Contains(expectedExceptionType, exceptionType, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    System.Management.Automation.Remoting.RemoteSessionHyperVSocketServer.ValidateToken(client, expectedToken, DateTimeOffset.UtcNow, 5);
                }
                System.Threading.Thread.Sleep(100); // Allow time for server to process
            }

            if (expectedMessage.Length == 0)
            {
                await serverTask;
            }
        }

        [SkippableFact]
        public async Task ValidateTokenTimeoutDoesAffectSession()
        {
            string token = "testToken";
            string expectedToken = token;
            int port = 50000 + (int)(DateTime.Now.Ticks % 10000);

            var expectedClientSends = new List<(string Message, Encoding Encoding, int DelayMs)>
            {
                (Message: "VERSION", Encoding: Encoding.ASCII, DelayMs: 1), // Response to VERSION
                (Message: "VERSION_2", Encoding: Encoding.ASCII, DelayMs: 1), // Response to VERSION_2
                (Message: $"TOKEN {token}", Encoding: Encoding.ASCII, DelayMs: 1),
                (Message: string.Empty, Encoding: Encoding.ASCII, DelayMs: 99), // Send some data after the handshake
                (Message: string.Empty, Encoding: Encoding.ASCII, DelayMs: 100), // Send some data after the handshake
                (Message: string.Empty, Encoding: Encoding.ASCII, DelayMs: 101),  // Send some data after the handshake
                (Message: string.Empty, Encoding: Encoding.ASCII, DelayMs: 102),  // Send some data after the handshake
                (Message: string.Empty, Encoding: Encoding.ASCII, DelayMs: 103),  // Send some data after the handshake
            };

            var serverResponses = new List<(string Message, Encoding Encoding)>{
                (Message: "VERSION_2", Encoding: Encoding.ASCII), // Response to VERSION_2
                (Message: "PASS", Encoding: Encoding.ASCII), // Response to VERSION_2
                (Message: "PASS", Encoding: Encoding.ASCII), // Response to token
                (Message: "PSRP-Message0", Encoding: Encoding.ASCII), // Indicate server is ready to receive data
                (Message: "PSRP-Message1", Encoding: Encoding.ASCII), // Indicate server is ready to receive data
                (Message: "PSRP-Message2", Encoding: Encoding.ASCII),  // Indicate server is ready to receive data
                (Message: "PSRP-Message3", Encoding: Encoding.ASCII),  // Indicate server is ready to receive data
                (Message: "PSRP-Message4", Encoding: Encoding.ASCII),
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var serverTask = Task.Run(() => StartHandshakeServer("Client", port, serverResponses, expectedClientSends, verifyConnectionClosed: false, cts.Token, sendFirst: true), cts.Token);

            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                ConnectWithRetry(client, IPAddress.Loopback, port, _output);
                System.Management.Automation.Remoting.RemoteSessionHyperVSocketServer.ValidateToken(client, expectedToken, DateTimeOffset.UtcNow, 5);
                for (int i = 0; i < 5; i++)
                {
                    System.Threading.Thread.Sleep(1500);
                    client.Send(Encoding.ASCII.GetBytes($"PSRP-Message{i}")); // Send some data after the handshake
                }
            }

            await serverTask;
        }

        [SkippableTheory]
        [InlineData("abc", "xyz")]
        [InlineData("abc", "abcdef")]
        [InlineData("abcdef", "abc")]
        [InlineData("abc\0def", "abc")]
        public async Task ValidateFailsWhenTokensMismatch(string token, string expectedToken)
        {
            int port = 50000 + (int)(DateTime.Now.Ticks % 10000);

            var expectedClientSends = new List<(string Message, Encoding Encoding)>
            {
                (Message: "VERSION", Encoding: Encoding.ASCII), // Initial request
                (Message: "VERSION_2", Encoding: Encoding.ASCII), // Response to VERSION_2
                (Message: $"TOKEN {token}", Encoding: Encoding.ASCII)
            };

            var serverResponses = new List<(string Message, Encoding Encoding)>
            {
                (Message: "VERSION_2", Encoding: Encoding.ASCII), // Response to VERSION
                (Message: "PASS", Encoding: Encoding.ASCII), // Response to VERSION_2
                (Message: "FAIL", Encoding: Encoding.ASCII) // Response to token
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var serverTask = Task.Run(() => StartHandshakeServer("Client", port, serverResponses, expectedClientSends, verifyConnectionClosed: true, cts.Token, sendFirst: true), cts.Token);

            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                ConnectWithRetry(client, IPAddress.Loopback, port, _output);
                DateTimeOffset tokenCreationTime = DateTimeOffset.UtcNow; // Token created 10 minutes ago
                var exception = Assert.Throws<System.Management.Automation.Remoting.PSDirectException>(
                    () => System.Management.Automation.Remoting.RemoteSessionHyperVSocketServer.ValidateToken(client, expectedToken, tokenCreationTime, 5));
                System.Threading.Thread.Sleep(100); // Allow time for server to process
                Assert.Contains("The credential is invalid.", exception.Message);
            }

            await serverTask;
        }
    }
}
