[Open Iconic v1.1.1](http://useiconic.com/open)
===========

### Open Iconic is the open source sibling of [Iconic](http://useiconic.com). It is a hyper-legible collection of 223 icons with a tiny footprint&mdash;ready to use with Bootstrap and Foundation. [View the collection](http://useiconic.com/open#icons)



## What's in Open Iconic?

* 223 icons designed to be legible down to 8 pixels
* Super-light SVG files - 61.8 for the entire set 
* SVG sprite&mdash;the modern replacement for icon fonts
* Webfont (EOT, OTF, SVG, TTF, WOFF), PNG and WebP formats
* Webfont stylesheets (including versions for Bootstrap and Foundation) in CSS, LESS, SCSS and Stylus formats
* PNG and WebP raster images in 8px, 16px, 24px, 32px, 48px and 64px.


## Getting Started

#### For code samples and everything else you need to get started with Open Iconic, check out our [Icons](http://useiconic.com/open#icons) and [Reference](http://useiconic.com/open#reference) sections.

### General Usage

#### Using Open Iconic's SVGs

We like SVGs and we think they're the way to display icons on the web. Since Open Iconic are just basic SVGs, we suggest you display them like you would any other image (don't forget the `alt` attribute).

```
<img src="/open-iconic/svg/icon-name.svg" alt="icon name">
```

#### Using Open Iconic's SVG Sprite

Open Iconic also comes in a SVG sprite which allows you to display all the icons in the set with a single request. It's like an icon font, without being a hack.

Adding an icon from an SVG sprite is a little different than what you're used to, but it's still a piece of cake. *Tip: To make your icons easily style able, we suggest adding a general class to the* `<svg>` *tag and a unique class name for each different icon in the* `<use>` *tag.*  

```
<svg class="icon">
  <use xlink:href="open-iconic.svg#account-login" class="icon-account-login"></use>
</svg>
```

Sizing icons only needs basic CSS. All the icons are in a square format, so just set the `<svg>` tag with equal width and height dimensions.

```
.icon {
  width: 16px;
  height: 16px;
}
```

Coloring icons is even easier. All you need to do is set the `fill` rule on the `<use>` tag.

```
.icon-account-login {
  fill: #f00;
}
```

To learn more about SVG Sprites, read [Chris Coyier's guide](http://css-tricks.com/svg-sprites-use-better-icon-fonts/).

#### Using Open Iconic's Icon Font...


##### …with Bootstrap

You can find our Bootstrap stylesheets in `font/css/open-iconic-bootstrap.{css, less, scss, styl}`


```
<link href="/open-iconic/font/css/open-iconic-bootstrap.css" rel="stylesheet">
```


```
<span class="oi oi-icon-name" title="icon name" aria-hidden="true"></span>
```

##### …with Foundation

You can find our Foundation stylesheets in `font/css/open-iconic-foundation.{css, less, scss, styl}`

```
<link href="/open-iconic/font/css/open-iconic-foundation.css" rel="stylesheet">
```


```
<span class="fi-icon-name" title="icon name" aria-hidden="true"></span>
```

##### …on its own

You can find our default stylesheets in `font/css/open-iconic.{css, less, scss, styl}`

```
<link href="/open-iconic/font/css/open-iconic.css" rel="stylesheet">
```

```
<span class="oi" data-glyph="icon-name" title="icon name" aria-hidden="true"></span>
```


## License

### Icons

All code (including SVG markup) is under the [MIT License](http://opensource.org/licenses/MIT).

### Fonts

All fonts are under the [SIL Licensed](http://scripts.sil.org/cms/scripts/page.php?item_id=OFL_web).
