<div align="center">

# Tunnelka

**Простой VPN-клиент для Windows.** Добавляешь ключ, выбираешь сервер, нажимаешь одну кнопку.

<table>
<tr>
<td align="center" width="340">
<b>★ РЕКОМЕНДУЕМ</b><br>
<a href="https://github.com/swagmachine999/tunnelka_proxy/releases/latest/download/Tunnelka-Setup.exe"><img height="52" alt="Для Windows (.exe)" src="https://img.shields.io/badge/%E2%AC%87%EF%B8%8F%20%D0%94%D0%BB%D1%8F%20Windows%20(.exe)-8B6CF0?style=for-the-badge"></a>
</td>
<td align="center" width="340">
<b>&nbsp;</b><br>
<a href="https://github.com/swagmachine999/tunnelka_proxy/releases/latest/download/Tunnelka-Portable.zip"><img height="52" alt="Портативная (.zip)" src="https://img.shields.io/badge/%F0%9F%93%A6%20%D0%9F%D0%BE%D1%80%D1%82%D0%B0%D1%82%D0%B8%D0%B2%D0%BD%D0%B0%D1%8F%20(.zip)-4A3E62?style=for-the-badge"></a>
</td>
</tr>
</table>

<img height="30" alt="Поддержка" src="https://img.shields.io/badge/%D0%9F%D0%BE%D0%B4%D0%B4%D0%B5%D1%80%D0%B6%D0%BA%D0%B0-Windows%2010%20x64%20%7C%2011%20x64-4A3E62?style=for-the-badge">
<a href="https://github.com/swagmachine999/tunnelka_proxy/releases"><img height="30" alt="Скачиваний" src="https://img.shields.io/github/downloads/swagmachine999/tunnelka_proxy/total?style=for-the-badge&color=E79AC4&label=%D0%A1%D0%BA%D0%B0%D1%87%D0%B8%D0%B2%D0%B0%D0%BD%D0%B8%D0%B9"></a>
<a href="https://github.com/swagmachine999/tunnelka_proxy/releases/latest"><img height="30" alt="Актуальная версия" src="https://img.shields.io/github/v/release/swagmachine999/tunnelka_proxy?style=for-the-badge&color=8B6CF0&label=%D0%90%D0%BA%D1%82%D1%83%D0%B0%D0%BB%D1%8C%D0%BD%D0%B0%D1%8F%20%D0%B2%D0%B5%D1%80%D1%81%D0%B8%D1%8F"></a>

</div>

---

## Быстрый старт

1. Нажми «Для Windows (.exe)» и запусти файл. Если Windows предупредит о неизвестном издателе, нажми «Подробнее», затем «Выполнить в любом случае».
2. Нажми «+» или Ctrl+V и вставь ключ от своего VPN-сервиса. Ключ в виде QR-кода — «Сканировать QR».
3. Выбери сервер или «Авто» и нажми большую круглую кнопку.

## Режимы

- **Прокси** — без прав администратора. Через VPN идут программы, которые используют системный прокси.
- **TUN** — весь трафик компьютера, включая игры и программы без настроек прокси. Нужны права администратора: при установке через установщик они спрашиваются один раз, дальше всё работает без запросов, в том числе после перезагрузки. У портативной версии права спрашиваются при каждом подключении.

## Возможности

- Ключи vless, vmess, trojan, shadowsocks, hysteria2, tuic и подписки по ссылке, ключи из QR-кода
- «Авто» в каждой подписке — самый быстрый сервер этого провайдера
- Пинг серверов по очереди сверху вниз, «Пинг всех» идёт по провайдерам по порядку
- Смена локации прямо во время подключения, без «отключить — включить»
- В режиме TUN при смене сервера соединения программ, которые идут мимо VPN (например, игры), не обрываются
- Автопереключение: если сервер перестал отвечать, Tunnelka сама переходит на другой из той же подписки. Включается и выключается в настройках
- Маршрутизация: список программ и сайтов, режимы «VPN для выбранных» и «Прямое для выбранных». Программы мимо VPN не зависят от работы сервера
- Kill switch: если VPN упал, интернет блокируется, пока не переподключишься
- Кнопка «Починить» в настройках: останавливает зависшее и снимает прокси и блокировку
- Защита: локальный прокси закрыт паролем, другие программы на компьютере им не воспользуются
- Статистика трафика, оверлей поверх игр по горячей клавише (по умолчанию Ctrl+Alt+P): пинг, скорость, потеря пакетов
- Предупреждение, когда подписка скоро закончится
- Запуск вместе с Windows и автоподключение
- Светлая и тёмная тема, русский и английский язык, масштаб интерфейса меняется сразу

## Обновления

Приложение само проверяет новые версии и предлагает установить. Проверить вручную можно в разделе «О приложении». Обновлять лучше через установщик: тогда не придётся заново давать права администратора.

## Системные требования

- Windows 10 или 11, 64-бит
- Процессор x64 (Intel или AMD)
- Около 300 МБ на диске и 300 МБ свободной памяти
- Экран от 1280×720

## Поддержка

- **Нашёл ошибку или есть идея** — создай [issue](https://github.com/swagmachine999/tunnelka_proxy/issues). Приложи версию из раздела «О приложении» и опиши, что произошло.
- **Быстрый вопрос** — напиши в [Telegram](https://t.me/swagmachine999).

Эти же ссылки есть в самом приложении: «О приложении», блок «Поддержка приложения».

## Поддержать проект

Tunnelka бесплатна и без рекламы. Если она тебе полезна, можно поддержать разработку. Это добровольно и ничего не открывает: все функции доступны всем.

| Монета | Сеть | Адрес |
|---|---|---|
| USDT | TON | `UQCZ7hwTng7bAmrgFc8iWGMDbOnJDPAMAAXXMGnOxXCNAtzX` |
| USDT | TRC20 (Tron) | `TH6XpkJ2VH5rpqRuHiVthszXhQuFhY68bU` |
| USDT и SOL | Solana | `DfnqFaNUMsh8WWcYDheg7otTtULjfXKZAB1VhmCVG28A` |
| BTC | Bitcoin | `bc1qvd32fxd5wp8f4xmn0pjkn9d7fyw4rlp7k8xuv7` |

Перед отправкой проверь, что выбрана именно указанная сеть: перевод по другой сети может потеряться.

## Основано на

[Xray-core](https://github.com/XTLS/Xray-core) и [sing-box](https://github.com/SagerNet/sing-box). Лицензии сторонних компонентов — в [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
