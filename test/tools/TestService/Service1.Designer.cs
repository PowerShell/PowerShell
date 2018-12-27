// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace TestService
{
    partial class Service1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.ServiceName = "Service1";
        }
   }
}
