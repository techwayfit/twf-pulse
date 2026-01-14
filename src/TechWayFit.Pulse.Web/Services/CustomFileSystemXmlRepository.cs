using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace TechWayFit.Pulse.Web.Services
{
    /// <summary>
    /// Custom XML repository for data protection keys with enhanced logging
    /// </summary>
    public class CustomFileSystemXmlRepository : IXmlRepository
    {
        private readonly DirectoryInfo _directory;
        private readonly ILogger<CustomFileSystemXmlRepository> _logger;

        public CustomFileSystemXmlRepository(DirectoryInfo directory, ILogger<CustomFileSystemXmlRepository> logger)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (!_directory.Exists)
            {
                _logger.LogWarning("Keys directory does not exist. Creating: {DirectoryPath}", _directory.FullName);
                _directory.Create();
            }

            _logger.LogInformation("CustomFileSystemXmlRepository initialized with directory: {DirectoryPath}", _directory.FullName);
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            try
            {
                _logger.LogInformation("GetAllElements called. Reading keys from: {DirectoryPath}", _directory.FullName);

                var files = _directory.GetFiles("*.xml", SearchOption.TopDirectoryOnly);
                _logger.LogInformation("Found {Count} key files", files.Length);

                var elements = new List<XElement>();

                foreach (var file in files)
                {
                    try
                    {
                        _logger.LogDebug("Reading key file: {FileName}", file.Name);
                        
                        var content = File.ReadAllText(file.FullName);
                        _logger.LogTrace("File content length: {Length} bytes", content.Length);

                        var document = XDocument.Parse(content);
                        var element = document.Root;

                        if (element != null)
                        {
                            // Log key details
                            var keyId = element.Attribute("id")?.Value;
                            var creationDate = element.Element("creationDate")?.Value;
                            var activationDate = element.Element("activationDate")?.Value;
                            var expirationDate = element.Element("expirationDate")?.Value;

                            _logger.LogInformation(
                                "Loaded key - ID: {KeyId}, Created: {CreationDate}, Activation: {ActivationDate}, Expiration: {ExpirationDate}",
                                keyId, creationDate, activationDate, expirationDate);

                            elements.Add(element);
                        }
                        else
                        {
                            _logger.LogWarning("Key file {FileName} has no root element", file.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading key file: {FileName}", file.Name);
                    }
                }

                _logger.LogInformation("Successfully loaded {Count} keys", elements.Count);
                return elements.AsReadOnly();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllElements");
                throw;
            }
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            try
            {
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                _logger.LogInformation("StoreElement called. FriendlyName: {FriendlyName}", friendlyName ?? "null");

                // Extract key details for logging
                var keyId = element.Attribute("id")?.Value ?? Guid.NewGuid().ToString();
                var fileName = $"key-{keyId}.xml";
                var filePath = Path.Combine(_directory.FullName, fileName);

                _logger.LogInformation("Storing key to: {FilePath}", filePath);

                var document = new XDocument(element);
                var xmlContent = document.ToString();
                
                _logger.LogTrace("XML content to write ({Length} bytes):\n{Content}", 
                    xmlContent.Length, xmlContent);

                File.WriteAllText(filePath, xmlContent);

                _logger.LogInformation("Successfully stored key: {KeyId} to {FileName}", keyId, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StoreElement. FriendlyName: {FriendlyName}", friendlyName);
                throw;
            }
        }
    }
}
