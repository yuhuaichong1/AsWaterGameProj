var _0x4eedc;
var cc__extends = __extends;
var cc__decorate = __decorate;
Object.defineProperty(exports, '__esModule', {
  value: true
});
exports.CollectPopup = undefined;
var $9BasePopup = require("BasePopup");
var $9List = require("List");
var $9NodeUtils = require("NodeUtils");
var $9CollectData = require("CollectData");
var $9GameEmun = require("GameEmun");
var $9CollectItem = require("CollectItem");
var cc__decorator = cc._decorator;
var ccp_ccclass = cc__decorator.ccclass;
cc__decorator.property;
var exp_CollectPopup = function (p0) {
  function _ctor() {
    var _Xg = null !== p0 && p0.apply(this, arguments) || this;
    _Xg.btnClose = null;
    _Xg.tabNode = null;
    _Xg.listView = null;
    _Xg.selectTabIndex = -1;
    return _Xg;
  }
  cc__extends(_ctor, p0);
  _ctor.prototype.load = function () {
    p0.prototype.load.call(this);
    this.initNode();
    this.initEvent();
    this.initBtn();
  };
  _ctor.prototype.start = function () {
    p0.prototype.start.call(this);
    this.switchTab($9GameEmun.CollectType.Drink);
  };
  _ctor.prototype.initNode = function () {
    this.btnClose = $9NodeUtils.NodeUtils.getChild("content/btnClose", this.node);
    this.tabNode = $9NodeUtils.NodeUtils.getChild("content/tabNode", this.node);
    this.listView = $9NodeUtils.NodeUtils.getChild("content/viewNode/scrollView", this.node).getComponent($9List.default);
  };
  _ctor.prototype.initEvent = function () {};
  _ctor.prototype.initBtn = function () {
    var _brm = this;
    this.tabNode.children.forEach(function (p1, p2) {
      _brm.onClickForTouchEnd(p1, function () {
        _brm.switchTab(p2);
      });
    });
    this.onClickForTouchEnd(this.btnClose, function () {
      _brm.hide();
    });
  };
  _ctor.prototype.onListLevelRender = function (p1, p2) {
    p1.getComponent($9CollectItem.CollectItem).initData(this.selectTabIndex, p2);
  };
  _ctor.prototype.switchTab = function (p1) {
    if (p1 !== this.selectTabIndex) {
      this.selectTabIndex = p1;
      this.setTabSelectState(p1);
      this.listView.numItems = p1 == $9GameEmun.CollectType.Drink ? $9CollectData.Collect_NaiCha.length : $9CollectData.Collect_TianPin.length;
    }
  };
  _ctor.prototype.setTabSelectState = function (p1) {
    for (var _bJc = 0; _bJc < this.tabNode.children.length; _bJc++) {
      var _bKI = this.tabNode.children[_bJc];
      _bKI && (_bKI.getComponent(cc.Sprite).enabled = p1 == _bJc);
    }
  };
  return cc__decorate([ccp_ccclass], _ctor);
}($9BasePopup.BasePopup);
exports.CollectPopup = exp_CollectPopup;