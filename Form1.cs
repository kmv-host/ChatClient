using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;         // Добавили пространство имен
using System.Net.Sockets; //Добавили пространство имен
using System.Threading;   //Добавили пространство имен

namespace ChatClient
{
    public partial class Form1 : Form
    {
        private TcpClient client;
        private NetworkStream stream;
        private bool isConnected = false;
        private readonly object lockObject = new object();

        public Form1()
        {
            InitializeComponent();
            btnDisconnect.Enabled = false;
            btnSend.Enabled = false;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Введите имя пользователя", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IPAddress.TryParse(txtIP.Text, out _) && !txtIP.Text.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Введите корректный IP-адрес или 'localhost'", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtPort.Text, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Введите корректный порт (1-65535)", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                client = new TcpClient();
                client.Connect(txtIP.Text, port);
                stream = client.GetStream();
                isConnected = true;

                // Отправляем имя серверу
                byte[] nameData = Encoding.UTF8.GetBytes($"NAME:{txtUsername.Text}");
                stream.Write(nameData, 0, nameData.Length);

                Thread receiveThread = new Thread(ReceiveMessages);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                UpdateUIOnConnect();
                UpdateChat($"Подключен к серверу {txtIP.Text}:{txtPort.Text} как {txtUsername.Text}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Disconnect();
            }
        }

        private void UpdateUIOnConnect()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateUIOnConnect));
                return;
            }

            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
            btnSend.Enabled = true;
            txtUsername.Enabled = false;
            txtIP.Enabled = false;
            txtPort.Enabled = false;
        }

        private void UpdateUIOnDisconnect()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateUIOnDisconnect));
                return;
            }

            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            btnSend.Enabled = false;
            txtUsername.Enabled = true;
            txtIP.Enabled = true;
            txtPort.Enabled = true;
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[4096]; // Увеличили размер буфера
            try
            {
                while (isConnected)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    UpdateChat(message);
                }
            }
            catch
            {
                UpdateChat("Соединение с сервером потеряно");
            }
            finally
            {
                Disconnect();
            }
        }

        private void UpdateChat(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateChat), message);
                return;
            }
            txtChat.AppendText($"{message}\r\n");
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (!isConnected) return;

            string message = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                lock (lockObject)
                {
                    stream.Write(data, 0, data.Length);
                }
                txtChat.AppendText($"{txtUsername.Text}: {message}\r\n");
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                UpdateChat($"Ошибка отправки: {ex.Message}");
                Disconnect();
            }
        }


        private void Disconnect()
        {
            if (!isConnected) return;

            isConnected = false;
            try
            {
                stream?.Close();
                client?.Close();
            }
            catch { }

            UpdateUIOnDisconnect();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Disconnect();
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                btnSend_Click(sender, e);
                e.SuppressKeyPress = true; // Предотвращаем звуковой сигнал
            }
        }

        private void btnDisconnect_Click_1(object sender, EventArgs e)
        {
            Disconnect();
            UpdateChat("Отключен от сервера");
        }
    }
}
