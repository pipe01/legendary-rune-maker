# Legendary Rune Maker
[![Build status](https://ci.appveyor.com/api/projects/status/u5y57w0cfpluaql0?svg=true)](https://ci.appveyor.com/project/pipe01/legendary-rune-maker)

[![Donate](https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif)](https://www.paypal.me/pipe01)

This is a fork of the original [Legendary Rune Maker](https://github.com/pipe01/legendary-rune-maker/), in order to use its internals in [RiftGG](https://rift.gg)

A rune maker for League of Legends that also offers a bunch of automation options. Download latest release [here](https://github.com/pipe01/legendary-rune-maker/releases/latest).

# Features
* Copy and create rune pages without the sluggishness of the LoL client.
* Automatically detect your LoL client without any setup required.
* Automatically picks the appropriate rune page when you lock in a champion in champ select.
* Easily load a rune page from various providers (shown [below](#providers)).
* Separate rune pages of a champion by lanes.
* Automatically update on startup (can be disabled on config file).
* Automatically accept ready check.
* Automatically picks champions and summoner spells and bans champions.
* (Down)load an appropiate item set automatically.
* Shows you how to level up your skills.
* Always up-to-date on the latest LoL patch.
* Share rune pages with a single short text string.

*Hint: hold shift when selecting a champion or position to copy the current page over*

# Providers

|             |       Rune pages      |       Item sets       |      Skill order      |       Stat runes      |
|-------------|:---------------------:|:---------------------:|:---------------------:|:---------------------:|
| Champion.GG | ![](./table_mark.png) | ![](./table_mark.png) | ![](./table_mark.png) | ![](./table_mark.png) |
| LoLFlavor   |                       | ![](./table_mark.png) |                       |                       |
| MetaLoL     | ![](./table_mark.png) | ![](./table_mark.png) | ![](./table_mark.png) |                       |
| Op.GG       | ![](./table_mark.png) |                       | ![](./table_mark.png) | ![](./table_mark.png) |
| Runes.lol   | ![](./table_mark.png) |                       |                       |                       |
| U.GG        | ![](./table_mark.png) | ![](./table_mark.png) | ![](./table_mark.png) | ![](./table_mark.png) |

# Screenshots

![](https://i.imgur.com/TuUbid4.png)
![](https://i.imgur.com/Ltv3490.png)

# Credits
* [@pipe01](https://github.com/pipe01/) for creating the original Legendary Rune Maker
* [@Sudravirodhin](https://github.com/sudravirodhin) for helping me a ton debugging the app and suggesting new features.
* [RuneBook](https://github.com/OrangeNote/RuneBook) and [Championify](https://github.com/dustinblackman/Championify) for inspiration and some provider reference.
* Riot for their amazing work on the client and its API.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. Just don't be stupid.
