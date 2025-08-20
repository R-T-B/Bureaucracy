using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Bureaucracy
{
    /// <summary>
    /// Provides integration with the Contract Configurator mod for KSP
    /// Uses reflection to safely access Contract Configurator's contract management
    /// </summary>
    public static class ContractConfiguratorBridge
    {
        #region Constants
        private const string CONTRACT_CONFIGURATOR_TYPE = "ContractConfigurator.ConfiguredContract";
        private const string CURRENT_CONTRACTS_PROPERTY = "CurrentContracts";
        private const string LOG_PREFIX = "[Bureaucracy]";
        #endregion

        #region Private Fields
        private static bool _isContractConfiguratorAvailable = false;
        private static Func<IEnumerable<object>> _getCurrentContractsDelegate;
        #endregion

        #region Public Properties
        /// <summary>
        /// Indicates whether Contract Configurator mod is loaded and accessible
        /// </summary>
        public static bool IsContractConfiguratorAvailable => _isContractConfiguratorAvailable;
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize the bridge by attempting to connect to Contract Configurator
        /// Should be called during KSP mod initialization
        /// </summary>
        public static void Initialize()
        {
            _isContractConfiguratorAvailable = TryConnectToContractConfigurator();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Retrieves all active Contract Configurator contracts
        /// </summary>
        /// <returns>List of active contracts, or empty list if Contract Configurator is unavailable</returns>
        public static List<Contracts.Contract> GetActiveContracts()
        {
            if (!_isContractConfiguratorAvailable || _getCurrentContractsDelegate == null)
            {
                LogMessage("Contract Configurator not available - returning empty contract list");
                return new List<Contracts.Contract>();
            }

            try
            {
                var contractObjects = _getCurrentContractsDelegate.Invoke();
                return ExtractValidContracts(contractObjects);
            }
            catch (Exception ex)
            {
                LogError($"Failed to retrieve contracts from Contract Configurator: {ex.Message}");
                return new List<Contracts.Contract>();
            }
        }
        #endregion

        #region Private Methods
        private static bool TryConnectToContractConfigurator()
        {
            try
            {
                var contractConfiguratorType = FindContractConfiguratorType();
                if (contractConfiguratorType == null)
                {
                    LogMessage($"Contract Configurator type '{CONTRACT_CONFIGURATOR_TYPE}' not found - mod may not be installed");
                    return false;
                }

                var contractsProperty = GetContractsProperty(contractConfiguratorType);
                if (contractsProperty == null)
                {
                    LogError($"Property '{CURRENT_CONTRACTS_PROPERTY}' not found in Contract Configurator");
                    return false;
                }

                if (!CreateContractsDelegate(contractsProperty))
                {
                    LogError("Failed to create delegate for Contract Configurator property access");
                    return false;
                }

                LogMessage("Successfully connected to Contract Configurator");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error connecting to Contract Configurator: {ex.Message}");
                return false;
            }
        }

        private static Type FindContractConfiguratorType()
        {
            Type foundType = null;

            AssemblyLoader.loadedAssemblies.TypeOperation(type =>
            {
                if (type.FullName == CONTRACT_CONFIGURATOR_TYPE)
                {
                    foundType = type;
                }
            });

            return foundType;
        }

        private static PropertyInfo GetContractsProperty(Type contractConfiguratorType)
        {
            return contractConfiguratorType.GetProperty(CURRENT_CONTRACTS_PROPERTY,
                BindingFlags.Public | BindingFlags.Static);
        }

        private static bool CreateContractsDelegate(PropertyInfo contractsProperty)
        {
            try
            {
                var getMethod = contractsProperty.GetGetMethod();
                if (getMethod == null) return false;

                _getCurrentContractsDelegate = (Func<IEnumerable<object>>)
                    Delegate.CreateDelegate(typeof(Func<IEnumerable<object>>), getMethod);

                return _getCurrentContractsDelegate != null;
            }
            catch
            {
                return false;
            }
        }

        private static List<Contracts.Contract> ExtractValidContracts(IEnumerable<object> contractObjects)
        {
            if (contractObjects == null)
                return new List<Contracts.Contract>();

            return contractObjects
                .Where(obj => obj != null)
                .OfType<Contracts.Contract>()
                .ToList();
        }

        private static void LogMessage(string message)
        {
            Debug.Log($"{LOG_PREFIX} {message}");
        }

        private static void LogError(string message)
        {
            Debug.LogError($"{LOG_PREFIX} {message}");
        }
        #endregion
    }
}