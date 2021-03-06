﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Security.Cryptography;
using System.IO;
using Exchange.DkimSigner;
using Exchange.DkimSigner.Properties;

namespace ConfigurationSettings
{
    public class DomainElement : ConfigurationElement
    {

        [ConfigurationProperty("Domain", DefaultValue = "", IsKey = true, IsRequired = true)]
        public string Domain
        {
            get { return (string)base["Domain"]; }
            set { base["Domain"] = value; }
        }

        [ConfigurationProperty("Selector", DefaultValue = "", IsRequired = true)]
        public string Selector
        {
            get { return (string)base["Selector"]; }
            set { base["Selector"] = value; }
        }

        [ConfigurationProperty("PrivateKeyFile", DefaultValue = "", IsRequired = true)]
        public string PrivateKeyFile
        {
            get { return (string)base["PrivateKeyFile"]; }
            set { base["PrivateKeyFile"] = value; }
        }

        /// <summary>
        /// Only for backwards compatibility. Use RecipientRule instead
        /// </summary>
        [ConfigurationProperty("Rule", DefaultValue = ".*", IsRequired = false)]
        [Obsolete("Use RecipientRule instead")]
        public string Rule
        {
            get { return (string)base["Rule"]; }
            set { base["Rule"] = value; }
        }

        [ConfigurationProperty("RecipientRule", DefaultValue = ".*", IsRequired = false)]
        public string RecipientRule
        {
            get { return (string)base["RecipientRule"]; }
            set { base["RecipientRule"] = value; }
        }

        [ConfigurationProperty("SenderRule", DefaultValue = ".*", IsKey = false, IsRequired = false)]
        public string SenderRule
        {
            get { return (string)this["SenderRule"]; }
            set { this["SenderRule"] = value; }
        }

        /// <summary>
        /// The RSA crypto service provider.
        /// </summary>
        public RSACryptoServiceProvider CryptoProvider
        {
            get { return cryptoProvider; }
        }
        private RSACryptoServiceProvider cryptoProvider;

        public bool initElement(string basePath)
        {
            string path;
            if (Path.IsPathRooted(PrivateKeyFile))
                path = PrivateKeyFile;
            else
            {
                path = Path.Combine(basePath, PrivateKeyFile);
            }
            if (!File.Exists(path))
            {
                Logger.LogError("PrivateKey for domain " + Domain + " not found: " + path);
                return false;
            }

            byte[] key = File.ReadAllBytes(path);
            switch (RSACryptoHelper.GetFormatFromEncodedRsaPrivateKey(key))
            {
                case RSACryptoFormat.PEM:
                    string pemkey = File.ReadAllText(path, Encoding.ASCII);
                    cryptoProvider = RSACryptoHelper.GetProviderFromPemEncodedRsaPrivateKey(pemkey);
                    break;
                case RSACryptoFormat.DER:
                    cryptoProvider = RSACryptoHelper.GetProviderFromDerEncodedRsaPrivateKey(key);
                    break;
                case RSACryptoFormat.XML:
                    string xmlkey = File.ReadAllText(path, Encoding.ASCII);
                    cryptoProvider = RSACryptoHelper.GetProviderFromXmlEncodedRsaPrivateKey(xmlkey);
                    break;
                default:
                    throw new ArgumentException(
                            Resources.CryptHelper_UnknownFormat,
                            "encodedKey");
            }

            return true;
        }

        internal string Key
        {
            get { return string.Format("{0}|{1}", Selector, Domain); }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DefaultDkimSigner"/> class.
        /// </summary>
        ~DomainElement()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.cryptoProvider != null)
                {
                    this.cryptoProvider.Clear();
                    this.cryptoProvider = null;
                }
            }
        }
    }
}
