namespace projekt
{
    partial class Form1
    {
        /// <summary>
        /// Wymagana zmienna projektanta.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Wyczyść wszystkie używane zasoby.
        /// </summary>
        /// <param name="disposing">prawda, jeżeli zarządzane zasoby powinny zostać zlikwidowane; Fałsz w przeciwnym wypadku.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Kod generowany przez Projektanta formularzy systemu Windows

        /// <summary>
        /// Metoda wymagana do obsługi projektanta — nie należy modyfikować
        /// jej zawartości w edytorze kodu.
        /// </summary>
        private void InitializeComponent()
        {
            this.CButton = new System.Windows.Forms.RadioButton();
            this.ASMButton = new System.Windows.Forms.RadioButton();
            this.LoadButton = new System.Windows.Forms.Button();
            this.StartButton = new System.Windows.Forms.Button();
            this.threadSlider = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.ThreadsNum = new System.Windows.Forms.Label();
            this.Indicator = new System.Windows.Forms.RadioButton();
            this.TimeLabel = new System.Windows.Forms.Label();
            this.Time = new System.Windows.Forms.Label();
            this.ReloadButton = new System.Windows.Forms.Button();
            this.RegularizationCheckbox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.threadSlider)).BeginInit();
            this.SuspendLayout();
            // 
            // CButton
            // 
            this.CButton.AutoSize = true;
            this.CButton.Location = new System.Drawing.Point(270, 157);
            this.CButton.Name = "CButton";
            this.CButton.Size = new System.Drawing.Size(39, 17);
            this.CButton.TabIndex = 0;
            this.CButton.TabStop = true;
            this.CButton.Text = "C#";
            this.CButton.UseVisualStyleBackColor = true;
            this.CButton.CheckedChanged += new System.EventHandler(this.CButton_CheckedChanged);
            // 
            // ASMButton
            // 
            this.ASMButton.AutoSize = true;
            this.ASMButton.Location = new System.Drawing.Point(270, 180);
            this.ASMButton.Name = "ASMButton";
            this.ASMButton.Size = new System.Drawing.Size(48, 17);
            this.ASMButton.TabIndex = 1;
            this.ASMButton.TabStop = true;
            this.ASMButton.Text = "ASM";
            this.ASMButton.UseVisualStyleBackColor = true;
            this.ASMButton.CheckedChanged += new System.EventHandler(this.ASMButton_CheckedChanged);
            // 
            // LoadButton
            // 
            this.LoadButton.Location = new System.Drawing.Point(42, 35);
            this.LoadButton.Name = "LoadButton";
            this.LoadButton.Size = new System.Drawing.Size(113, 23);
            this.LoadButton.TabIndex = 3;
            this.LoadButton.Text = "Załaduj dane";
            this.LoadButton.UseVisualStyleBackColor = true;
            this.LoadButton.Click += new System.EventHandler(this.LoadButton_Click);
            // 
            // StartButton
            // 
            this.StartButton.Location = new System.Drawing.Point(270, 203);
            this.StartButton.Name = "StartButton";
            this.StartButton.Size = new System.Drawing.Size(75, 23);
            this.StartButton.TabIndex = 4;
            this.StartButton.Text = "Procesuj";
            this.StartButton.UseVisualStyleBackColor = true;
            this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // threadSlider
            // 
            this.threadSlider.Location = new System.Drawing.Point(25, 382);
            this.threadSlider.Name = "threadSlider";
            this.threadSlider.Size = new System.Drawing.Size(364, 45);
            this.threadSlider.TabIndex = 5;
            this.threadSlider.Scroll += new System.EventHandler(this.threadSlider_Scroll);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label1.Location = new System.Drawing.Point(38, 338);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 20);
            this.label1.TabIndex = 6;
            this.label1.Text = "Wątki:";
            // 
            // ThreadsNum
            // 
            this.ThreadsNum.AutoSize = true;
            this.ThreadsNum.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.ThreadsNum.Location = new System.Drawing.Point(103, 334);
            this.ThreadsNum.Name = "ThreadsNum";
            this.ThreadsNum.Size = new System.Drawing.Size(16, 24);
            this.ThreadsNum.TabIndex = 7;
            this.ThreadsNum.Text = "-";
            // 
            // Indicator
            // 
            this.Indicator.AutoCheck = false;
            this.Indicator.AutoSize = true;
            this.Indicator.Cursor = System.Windows.Forms.Cursors.No;
            this.Indicator.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Indicator.ForeColor = System.Drawing.SystemColors.Desktop;
            this.Indicator.Location = new System.Drawing.Point(53, 64);
            this.Indicator.Name = "Indicator";
            this.Indicator.Size = new System.Drawing.Size(84, 17);
            this.Indicator.TabIndex = 8;
            this.Indicator.TabStop = true;
            this.Indicator.Text = "Brak danych";
            this.Indicator.UseVisualStyleBackColor = true;
            // 
            // TimeLabel
            // 
            this.TimeLabel.AutoSize = true;
            this.TimeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.TimeLabel.Location = new System.Drawing.Point(53, 139);
            this.TimeLabel.Name = "TimeLabel";
            this.TimeLabel.Size = new System.Drawing.Size(123, 20);
            this.TimeLabel.TabIndex = 9;
            this.TimeLabel.Text = "Czas wykonania";
            // 
            // Time
            // 
            this.Time.AutoSize = true;
            this.Time.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.Time.Location = new System.Drawing.Point(82, 170);
            this.Time.Name = "Time";
            this.Time.Size = new System.Drawing.Size(37, 16);
            this.Time.TabIndex = 10;
            this.Time.Text = "--:--:--";
            // 
            // ReloadButton
            // 
            this.ReloadButton.Location = new System.Drawing.Point(161, 35);
            this.ReloadButton.Name = "ReloadButton";
            this.ReloadButton.Size = new System.Drawing.Size(102, 23);
            this.ReloadButton.TabIndex = 11;
            this.ReloadButton.Text = "Załaduj ponownie";
            this.ReloadButton.UseVisualStyleBackColor = true;
            this.ReloadButton.Click += new System.EventHandler(this.ReloadButton_Click);
            // 
            // RegularizationCheckbox
            // 
            this.RegularizationCheckbox.AutoSize = true;
            this.RegularizationCheckbox.Location = new System.Drawing.Point(53, 97);
            this.RegularizationCheckbox.Name = "RegularizationCheckbox";
            this.RegularizationCheckbox.Size = new System.Drawing.Size(93, 17);
            this.RegularizationCheckbox.TabIndex = 12;
            this.RegularizationCheckbox.Text = "Regularyzacja";
            this.RegularizationCheckbox.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(405, 450);
            this.Controls.Add(this.RegularizationCheckbox);
            this.Controls.Add(this.ReloadButton);
            this.Controls.Add(this.Time);
            this.Controls.Add(this.TimeLabel);
            this.Controls.Add(this.Indicator);
            this.Controls.Add(this.ThreadsNum);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.threadSlider);
            this.Controls.Add(this.StartButton);
            this.Controls.Add(this.LoadButton);
            this.Controls.Add(this.ASMButton);
            this.Controls.Add(this.CButton);
            this.Name = "Form1";
            this.Text = "Cholesky";
            ((System.ComponentModel.ISupportInitialize)(this.threadSlider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton CButton;
        private System.Windows.Forms.RadioButton ASMButton;
        private System.Windows.Forms.Button LoadButton;
        private System.Windows.Forms.Button StartButton;
        private System.Windows.Forms.TrackBar threadSlider;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label ThreadsNum;
        private System.Windows.Forms.RadioButton Indicator;
        private System.Windows.Forms.Label TimeLabel;
        private System.Windows.Forms.Label Time;
        private System.Windows.Forms.Button ReloadButton;
        private System.Windows.Forms.CheckBox RegularizationCheckbox;
    }
}

