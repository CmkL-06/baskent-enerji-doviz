#!/usr/bin/env python3
"""
QuantaQuokka - Antigravity kullanım örneği
Python standart kütüphanesindeki antigravity modülü.
- import sırasında XKCD #353 tarayıcıda açılır.
- geohash(): XKCD #426 Munroe algoritması.
"""

import antigravity


def main():
    # import antigravity yukarıda zaten tarayıcıda XKCD #353'ü açar
    print("Antigravity kullanıldı (XKCD comic tarayıcıda açılmış olmalı).\n")

    # Geohash örneği (XKCD #426) - sonuç modül içinde print edilir
    # Koordinat: 37.421542, -122.085589 | Tarih: 2005-05-26-10458.68
    print("Geohash (Munroe algoritması) sonucu:")
    antigravity.geohash(37.421542, -122.085589, b"2005-05-26-10458.68")


if __name__ == "__main__":
    main()
