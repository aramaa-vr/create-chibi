---
layout: minimal
title: おちびちゃんズ化ツール（Ochibi-chans Converter Tool）
description: VRChatアバターを「おちびちゃんズ」向けに変換するUnity Editor拡張の導入・使い方・対応一覧・トラブル対応。
nav_order: 1
---

![おちびちゃんズ化ツールのヒーロー画像]({{ "/assets/img/ochibi/hero.webp" | relative_url }}){: .home-hero-img }

<div class="landing-hero" markdown="1">

# おちびちゃんズ化ツール（Ochibi-chans Converter Tool）

VRChatアバターを **「おちびちゃんズ」向けに変換**する Unity Editor 拡張です。  
**元アバターを壊さない（Ctrl+D相当で複製 → 変換）** 方式なので、普段の改変フローにそのまま組み込めます。

<div class="hero-actions">
  <a class="btn btn-primary" href="https://aramaa-vr.github.io/vpm-repos/redirect.html" target="_blank" rel="noopener noreferrer">➕ Add to VCC</a>
  <a class="btn" href="{{ "/quickstart/" | relative_url }}">🚀 クイックスタート</a>
  <a class="btn" href="{{ "/install/" | relative_url }}">📦 導入</a>
  <a class="btn" href="{{ "/usage/" | relative_url }}">🧸 使い方</a>
</div>

</div>

<div class="info-grid">
  <div class="info-card">
    <h3>できること</h3>
    <p>Hierarchyのアバターを複製し、変換先おちびちゃんズの設定（FX / Expressions / ViewPosition 等）を同期します。</p>
  </div>
  <div class="info-card">
    <h3>ずれ対策</h3>
    <p>髪・小物がずれるケース向けに、<strong>MA Bone Proxy</strong>想定の調整オプションを用意しています。</p>
  </div>
  <div class="info-card">
    <h3>対応一覧</h3>
    <p>変換元アバター／変換先おちびちゃんズは一覧ページに集約しています（長いので検索も便利）。</p>
  </div>
</div>

<div class="step-list">
  <div class="step-item"><strong>1) 導入</strong> VCCにリポジトリを追加し、パッケージをインストール</div>
  <div class="step-item"><strong>2) 変換</strong> Hierarchyで元アバターを選び、ツールから実行</div>
  <div class="step-item"><strong>3) 確認</strong> Consoleエラー／見た目／挙動をチェックしてアップロード</div>
</div>

<div class="notice-callout">
  <strong>最初に知っておくと安全です</strong>
  変換は <em>複製物</em> に対して行われ、複製後は<strong>Blueprint ID をクリア</strong>します（元アバターのIDを引き継ぎません）。
</div>

<div class="quick-links">
  <div class="quick-link"><a href="{{ "/quickstart/" | relative_url }}">🚀 クイックスタート</a><br/><span class="sub">最短で動かす手順だけ</span></div>
  <div class="quick-link"><a href="{{ "/support/" | relative_url }}">📚 対応一覧</a><br/><span class="sub">対応アバター／おちびちゃんズ</span></div>
  <div class="quick-link"><a href="{{ "/faq/" | relative_url }}">❓ FAQ</a><br/><span class="sub">よくある質問の短い答え</span></div>
  <div class="quick-link"><a href="{{ "/troubleshooting/" | relative_url }}">🧯 困ったとき</a><br/><span class="sub">症状別の切り分け</span></div>
</div>

---

## 公式リンク

- **Booth**: <https://aramaa.booth.pm/items/7906711>
- **GitHub**: <https://github.com/aramaa-vr/ochibi-chans-converter-tool>
- **Discord**（不具合報告・質問）: <https://discord.gg/BJ3BpVnMna>

