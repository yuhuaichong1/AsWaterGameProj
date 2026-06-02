var _0xa37ccf2;
var cc__extends = __extends;
var cc__decorate = __decorate;
Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.CollectItem = undefined;
var $9ToastManager = require("ToastManager");
var $9BaseComponent = require("BaseComponent");
var $9BundleUtils = require("BundleUtils");
var $9NodeUtils = require("NodeUtils");
var $9UIConfig = require("UIConfig");
var $9CollectData = require("CollectData");
var $9GameEmun = require("GameEmun");
var $9GameUser = require("GameUser");
var cc__decorator = cc._decorator;
var ccp_ccclass = cc__decorator.ccclass;
cc__decorator.property;
var exp_CollectItem = function (p0) {
  function _ctor() {
    var _baB = null !== p0 && p0.apply(this, arguments) || this;
    _baB.iconNode = null;
    _baB.nameLabel = null;
    _baB.noLabel = null;
    _baB.lockNode = null;
    _baB.isLock = false;
    return _baB;
  }
  cc__extends(_ctor, p0);
  _ctor.prototype.load = function () {
    p0.prototype.load.call(this);
    this.initNode();
    this.initBtn();
  };
  _ctor.prototype.initNode = function () {
    this.iconNode = $9NodeUtils.NodeUtils.getChild("icon", this.node);
    this.nameLabel = $9NodeUtils.NodeUtils.getChild('name', this.node).getComponent(cc.Label);
    this.noLabel = $9NodeUtils.NodeUtils.getChild('no', this.node).getComponent(cc.Label);
    this.lockNode = $9NodeUtils.NodeUtils.getChild("lock", this.node);
  };
  _ctor.prototype.initBtn = function () {
    var _brG = this;
    this.onClickForTouchEnd(this.node, function () {
      _brG.isLock && $9ToastManager.ToastManager.getInstance().show("通关解锁新收藏");
    });
  };
  _ctor.prototype.initData = function (p1, p2) {
    var _bwx = this;
    var _bwZ = (p1 == $9GameEmun.CollectType.Drink ? $9CollectData.Collect_NaiCha : $9CollectData.Collect_TianPin)[p2];
    if (_bwZ) {
      this.noLabel.string = "No." + _bwZ.id;
      this.nameLabel.string = _bwZ.name;
      $9BundleUtils.BundleUtils.loadBundleInnerResByBundleNameSync($9UIConfig.BundleName.Collect, _bwZ.name, cc.SpriteFrame).then(function (p3) {
        p3 && (_bwx.iconNode.getComponent(cc.Sprite).spriteFrame = p3);
      });
      var _bGW = _bwZ.unlock > $9GameUser.GameUser.getInstance().currentLevel;
      this.lockNode.active = _bGW;
      this.iconNode.color = _bGW ? cc.Color.BLACK : cc.Color.WHITE;
      this.isLock = _bGW;
    }
  };
  return cc__decorate([ccp_ccclass], _ctor);
}($9BaseComponent.BaseComponent);
exports.CollectItem = exp_CollectItem;