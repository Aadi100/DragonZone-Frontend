using System;
using System.IO.Ports;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace POS.Services
{
    public class SerialPortService : IDisposable
    {
        private readonly SerialPort _port;
        private readonly ILogger<SerialPortService> _logger;
        private readonly ConcurrentQueue<string> _dataQueue = new ConcurrentQueue<string>();
        private bool _disposed = false;

        public SerialPortService(ILogger<SerialPortService> logger)
        {
            _logger = logger;
            _port = new SerialPort("COM20", 9600, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            try
            {
                _port.Open();
                _logger.LogInformation("Serial port opened.");
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogError("Access to the port is denied. Please check if the device is connected.");
            }
            catch (IOException)
            {
                _logger.LogError("Port is not available. Please connect the device.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while opening the serial port.");
            }

            _port.DataReceived += Port_DataReceived;
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = _port.ReadExisting();
                _dataQueue.Enqueue(data);
                _logger.LogInformation($"Data Received: {data}");
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning("Read operation timed out.", ex);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "I/O error while reading from serial port.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from serial port.");
            }
        }

        public string GetLatestData()
        {
            return _dataQueue.TryDequeue(out var data) ? data : null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _port.DataReceived -= Port_DataReceived;
                    if (_port.IsOpen)
                    {
                        _port.Close();
                        _logger.LogInformation("Serial port closed.");
                    }
                    _port.Dispose();
                }
                _disposed = true;
            }
        }
    }
}