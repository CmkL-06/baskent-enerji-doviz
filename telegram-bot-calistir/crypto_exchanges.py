"""
Crypto Exchange API Integration
Supports: Binance (extensible for Gate.io, MEXC)
"""

import hmac
import hashlib
import requests
import time
import logging
from datetime import datetime, timedelta
from typing import Dict, List, Optional, Tuple

logger = logging.getLogger(__name__)


class BinanceAPI:
    """Binance API integration for checking deposits"""

    BASE_URL = "https://api.binance.com"

    # Network name mappings (our naming → Binance naming)
    NETWORK_MAP = {
        'TRC20': ['TRX', 'TRON', 'TRC20'],
        'BEP20': ['BSC', 'BEP20', 'BINANCE'],
        'ERC20': ['ETH', 'ERC20', 'ETHEREUM'],
    }

    NETWORK_TO_BINANCE = {
        'TRC20': 'TRX',
        'BEP20': 'BSC',
        'ERC20': 'ETH',
    }

    def __init__(self, api_key: str, api_secret: str):
        self.api_key = api_key
        self.api_secret = api_secret

    def _sign(self, params: dict) -> str:
        """Create HMAC-SHA256 signature for Binance API"""
        query_string = '&'.join(f"{k}={v}" for k, v in params.items())
        signature = hmac.new(
            self.api_secret.encode('utf-8'),
            query_string.encode('utf-8'),
            hashlib.sha256
        ).hexdigest()
        return signature

    def _request(self, method: str, endpoint: str, params: dict) -> Optional[any]:
        """Make signed API request to Binance"""
        params["timestamp"] = int(time.time() * 1000)
        params["signature"] = self._sign(params)

        headers = {"X-MBX-APIKEY": self.api_key}

        try:
            response = requests.request(
                method,
                f"{self.BASE_URL}{endpoint}",
                params=params,
                headers=headers,
                timeout=10
            )

            if response.status_code == 200:
                return response.json()
            else:
                logger.error(f"Binance API error ({response.status_code}): {response.text}")
                return None

        except requests.Timeout:
            logger.error("Binance API timeout")
            return None
        except requests.ConnectionError:
            logger.error("Binance API connection error")
            return None
        except requests.RequestException as e:
            logger.error(f"Binance API request failed: {e}")
            return None

    def get_deposit_history(self, coin: str = "USDT", limit: int = 100,
                           start_time: int = None) -> List[Dict]:
        """Get deposit history from Binance"""
        if not start_time:
            start_time = int((datetime.now() - timedelta(hours=24)).timestamp() * 1000)

        params = {
            "coin": coin,
            "limit": limit,
            "startTime": start_time,
        }

        result = self._request("GET", "/sapi/v1/capital/deposit/hisrec", params)
        return result if result is not None else []

    def verify_deposit(self, txid: str, address: str, amount: float, network: str,
                       time_limit_minutes: int = 120) -> Tuple[bool, Optional[Dict]]:
        """
        Verify a specific deposit by TXID, address, and amount.
        Returns (is_valid, deposit_info_or_error)
        """
        deposits = self.get_deposit_history(coin="USDT", limit=100)

        logger.info(f"Searching deposit: TXID={txid}, Amount={amount}, Address={address}")
        logger.info(f"Found {len(deposits)} deposits to check")

        found_deposit = None

        for deposit in deposits:
            dep_txid = deposit.get('txId', '')
            dep_amount = float(deposit.get('amount', 0))
            dep_address = deposit.get('address', '')

            # Match 1: exact TXID match
            if dep_txid and dep_txid.lower() == txid.lower():
                logger.info(f"Found by TXID match: {dep_txid}")
                found_deposit = deposit
                break

            # Match 2: amount + address match (for off-chain / internal transfers)
            if abs(dep_amount - amount) < 0.01 and dep_address.lower() == address.lower():
                insert_time = deposit.get('insertTime', 0)
                deposit_time = datetime.fromtimestamp(insert_time / 1000)
                time_diff = datetime.now() - deposit_time
                logger.info(f"Amount+Address match. Time diff: {time_diff}")
                if time_diff <= timedelta(minutes=time_limit_minutes):
                    found_deposit = deposit
                    break

        if found_deposit:
            return self._validate_deposit(found_deposit, txid, address, amount,
                                          network, time_limit_minutes)

        # Secondary check: address match with amount mismatch → report discrepancy
        for deposit in deposits:
            dep_amount = float(deposit.get('amount', 0))
            dep_address = deposit.get('address', '')

            if dep_address.lower() == address.lower():
                min_acceptable = amount * 0.995
                max_acceptable = amount + 1.0
                if dep_amount < min_acceptable or dep_amount > max_acceptable:
                    return False, {
                        "error": f"⚠️ TUTAR UYUŞMAZLIĞI! "
                                 f"Beklenen: {amount} USDT, Gönderilen: {dep_amount} USDT"
                    }

        return False, {"error": "İşlem bulunamadı. TXID'yi kontrol edip tekrar deneyin."}

    def _validate_deposit(self, deposit: Dict, txid: str, address: str,
                          amount: float, network: str,
                          time_limit_minutes: int) -> Tuple[bool, Dict]:
        """Validate a found deposit against expected values"""

        # Time check
        insert_time = deposit.get('insertTime', 0)
        deposit_time = datetime.fromtimestamp(insert_time / 1000)
        time_diff = datetime.now() - deposit_time

        if time_diff > timedelta(minutes=time_limit_minutes):
            return False, {"error": f"İşlem {time_limit_minutes} dakikadan eski"}

        # Address check
        if deposit.get('address', '').lower() != address.lower():
            return False, {"error": "Yatırım adresi eşleşmiyor"}

        # Amount check (tolerance: 0.5% less for fees, 1 USDT more)
        deposit_amount = float(deposit.get('amount', 0))
        min_acceptable = amount * 0.995
        max_acceptable = amount + 1.0

        if deposit_amount < min_acceptable:
            shortage = amount - deposit_amount
            return False, {
                "error": f"⚠️ EKSİK TUTAR! Beklenen: {amount} USDT, "
                         f"Gönderilen: {deposit_amount} USDT. Eksik: {shortage:.2f} USDT"
            }
        elif deposit_amount > max_acceptable:
            excess = deposit_amount - amount
            return False, {
                "error": f"⚠️ FAZLA TUTAR! Beklenen: {amount} USDT, "
                         f"Gönderilen: {deposit_amount} USDT. Fazla: {excess:.2f} USDT"
            }

        # Network check
        deposit_network = deposit.get('network', '')
        if not self._match_network(network, deposit_network):
            return False, {"error": f"Ağ uyuşmazlığı: beklenen {network}, gelen {deposit_network}"}

        # Status check (1 = success/confirmed)
        if deposit.get('status') != 1:
            return False, {"error": "Yatırım henüz onaylanmadı"}

        # Confirmations
        confirmations = self._parse_confirmations(deposit)

        return True, {
            "txid": deposit.get('txId') or txid,
            "amount": deposit_amount,
            "network": deposit_network,
            "address": deposit.get('address'),
            "time": deposit_time.isoformat(),
            "confirmations": confirmations,
            "is_offchain": not bool(deposit.get('txId')),
        }

    def _match_network(self, expected: str, actual: str) -> bool:
        """Check if network names match (handles different naming conventions)"""
        expected_upper = expected.upper()
        actual_upper = actual.upper()

        if expected_upper in self.NETWORK_MAP:
            return actual_upper in self.NETWORK_MAP[expected_upper]

        return expected_upper == actual_upper

    def _parse_confirmations(self, deposit: Dict) -> int:
        """Parse confirmation count from deposit data"""
        if deposit.get('status') == 1:
            # status=1 means fully confirmed
            return 10

        confirm_times = deposit.get('confirmTimes', '0/0')
        if isinstance(confirm_times, str) and '/' in confirm_times:
            try:
                current, _ = confirm_times.split('/')
                return int(current)
            except (ValueError, TypeError):
                return 0

        return 0

    def get_deposit_address(self, coin: str = "USDT", network: str = "TRC20") -> Optional[str]:
        """Get deposit address for a specific coin and network"""
        params = {
            "coin": coin,
            "network": self.NETWORK_TO_BINANCE.get(network.upper(), network),
        }

        result = self._request("GET", "/sapi/v1/capital/deposit/address", params)
        if result:
            return result.get('address')
        return None


class CryptoExchangeManager:
    """Manager for multiple exchange APIs per dealer"""

    def __init__(self):
        self.exchanges: Dict[str, BinanceAPI] = {}

    def add_exchange(self, dealer_id: int, exchange_name: str,
                     api_key: str, api_secret: str):
        """Register an exchange API for a dealer"""
        key = f"{dealer_id}_{exchange_name.lower()}"

        if exchange_name.lower() == "binance":
            self.exchanges[key] = BinanceAPI(api_key, api_secret)
        else:
            raise ValueError(f"Unsupported exchange: {exchange_name}")

    def get_exchange(self, dealer_id: int, exchange_name: str) -> Optional[BinanceAPI]:
        """Get exchange API instance for a dealer"""
        key = f"{dealer_id}_{exchange_name.lower()}"
        return self.exchanges.get(key)

    def verify_deposit(self, dealer_id: int, exchange_name: str, txid: str,
                       address: str, amount: float, network: str) -> Tuple[bool, Optional[Dict]]:
        """Verify a deposit for a specific dealer's exchange"""
        exchange = self.get_exchange(dealer_id, exchange_name)

        if not exchange:
            return False, {"error": "Bu bayi için borsa yapılandırılmamış"}

        return exchange.verify_deposit(txid, address, amount, network)

    def get_deposit_history(self, dealer_id: int, exchange_name: str) -> List[Dict]:
        """Get deposit history for a dealer's exchange"""
        exchange = self.get_exchange(dealer_id, exchange_name)

        if not exchange:
            return []

        return exchange.get_deposit_history()
