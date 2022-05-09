using DG.Tweening;
using SimpleMan.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ToyTanks.LevelEditor.UI {
    public class EditorUI : MonoBehaviour {

        public LevelEditor editor;
        public CanvasGroup uiCanvasGroup;
        public Button playButton;
        public Button actionButton;
        public Button saveButton;
        public Sprite playSprite;
        public Sprite pauseSprite;
        public Sprite destroySprite;
        public Sprite buildSprite;
        public Sprite viewSprite;
        public Sprite moveSprite;
        public Sprite tankSprite;
        public Sprite terraLevelZeroSprite;
        public Sprite terraLevelOneSprite;
        public Sprite terraLevelMinusSprite;
        public Image playButtonIcon;
        public Image actionButtonIcon;
        public RectTransform extendedOptions;
        public RectTransform extenedOptionsButton;
        public RectTransform optionsButton;
        public Slider zoomSlider;
        public ScrollRect assetScrollRect;
        public SelectItem selectItem;
        public TMP_Text gridSizeSliderTitle;
        public List<SelectItem> themeItems;
        public List<TranslucentButton> assetTabButtons;
        public List<SelectItem> assetItems;
        public List<SelectItem> gridSizeItems;
        private bool extendedOptionsShown = false;
        const float toggleEditorUIDuration = 1f;

		private void Awake() {
            uiCanvasGroup.alpha = 0;
            uiCanvasGroup.gameObject.SetActive(false);
		}

		public void Save() {
            if(editor.loadedLevelId <= 4096) {
#if UNITY_EDITOR
                editor.SaveAsOfficialLevel();
#endif
            } else {
                //editor.SaveCustomLevel();
			}
            saveButton.interactable = false;
            saveButton.transform.GetChild(0).DOLocalRotate(new Vector3(0, 0, 360), 1f, RotateMode.FastBeyond360).SetEase(Ease.OutBounce).OnComplete(() => {
                saveButton.interactable = true;
			});
		}

        public void ToggleBuildMode() {
			switch(editor.buildMode) {
				case LevelEditor.BuildMode.View:
                    editor.buildMode = LevelEditor.BuildMode.Build;
					break;
				case LevelEditor.BuildMode.Move:
					break;
                case LevelEditor.BuildMode.Build:
                case LevelEditor.BuildMode.BuildExtra:
                case LevelEditor.BuildMode.Tanks:
                case LevelEditor.BuildMode.TerraformDown:
                case LevelEditor.BuildMode.TerraformUp:
                case LevelEditor.BuildMode.TerraBase:
                case LevelEditor.BuildMode.PlaceFlora:
                    editor.buildMode = LevelEditor.BuildMode.Destroy;
                    break;
				case LevelEditor.BuildMode.Destroy:
                    editor.buildMode = LevelEditor.BuildMode.View;
                    break;
			}

            RenderAssets();
            this.Delay(0.12f, () => {
                UpdateActionButton();
                actionButton.transform.GetChild(0).localScale = new Vector3(-1f, 1, 1);
            });
            actionButton.RectTransform().DOLocalRotate(new Vector3(0, 180, 0), 0.25f, RotateMode.FastBeyond360).SetEase(Ease.OutBounce).OnComplete(() => {
                actionButton.RectTransform().localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                actionButton.transform.GetChild(0).localScale = new Vector3(1f, 1, 1);
            });
		}

        public void UpdateActionButton() {
            actionButtonIcon.color = Color.white;
            switch(editor.buildMode) {
				case LevelEditor.BuildMode.View:
                    actionButtonIcon.sprite = viewSprite;
                    actionButtonIcon.color = new Color(0.121f, 0.537f, 1f);
                    break;
				case LevelEditor.BuildMode.Move:
                    actionButtonIcon.sprite = moveSprite;
                    actionButtonIcon.color = new Color(0.121f, 0.537f, 1f);
                    break;
				case LevelEditor.BuildMode.Build:
                case LevelEditor.BuildMode.BuildExtra:
                    actionButtonIcon.sprite = buildSprite;
                    actionButtonIcon.color = new Color(0.537f, 1f, 0.121f);
                    break;
				case LevelEditor.BuildMode.Tanks:
                    actionButtonIcon.sprite = tankSprite;
                    actionButtonIcon.color = new Color(0.537f, 1f, 0.121f);
                    break;
				case LevelEditor.BuildMode.Destroy:
                    actionButtonIcon.sprite = destroySprite;
                    actionButtonIcon.color = new Color(1, 0.203f, 0.121f);
                    break;
                case LevelEditor.BuildMode.TerraBase:
                    actionButtonIcon.sprite = terraLevelZeroSprite;
                    actionButtonIcon.color = new Color(0.121f, 0.537f, 1f);
                    break;
                case LevelEditor.BuildMode.TerraformUp:
                    actionButtonIcon.sprite = terraLevelOneSprite;
                    actionButtonIcon.color = new Color(0.537f, 1f, 0.121f);
                    break;
				case LevelEditor.BuildMode.TerraformDown:
                    actionButtonIcon.sprite = terraLevelMinusSprite;
                    actionButtonIcon.color = new Color(1, 0.203f, 0.121f);
                    break;
				default:
					break;
			}
		}

		public void ToggleExtendedOptions() {
            if(extendedOptionsShown) {
                extendedOptionsShown = false;
                extendedOptions.DOAnchorPosX(-475, 0.25f, true).SetEase(Ease.OutBounce);
                extenedOptionsButton.DOLocalRotate(new Vector3(0, 0, 0), 0.3f);
            } else {
                extendedOptionsShown = true;
                extendedOptions.DOAnchorPosX(0, 0.25f, true).SetEase(Ease.OutExpo);
                extenedOptionsButton.DOLocalRotate(new Vector3(0, 0, 180), 0.3f);
            }
        }

        public void RenderThemeItems() {
            int num = 0;
            foreach(var item in themeItems) {
                item.Deselect();
                item.value = num;
                item.SetText((WorldTheme)num + "");
                item.SetOnClick(() => {
                    item.Select();
                    editor.SwitchTheme((WorldTheme)item.value);
                    RenderThemeItems();
                });
                num++;
            }
            themeItems[(int)editor.theme].Select();
		}

        public void RenderGridSizeItems() {
            int num = 0;
            foreach(var item in gridSizeItems) {
                item.Deselect();
                item.value = num;
                item.SetText(LevelManager.GetGridBoundaryText((GridSizes)num));
                item.SetOnClick(() => {
                    editor.SwitchGridSize((GridSizes)item.value, false);
                    RenderGridSizeItems();
                });
                num++;
			}
            gridSizeSliderTitle.SetText("Layout: " + LevelManager.GetGridBoundaryText(editor.gridSize));
            gridSizeItems[(int)editor.gridSize].Select();
        }

        public void RenderTabAsset() {
            int num = 0;
            foreach(var item in assetTabButtons) {
                item.value = num;
                item.Deselect();
                item.SetOnClick(() => {
                    item.Select();
                    editor.assetView = (LevelEditor.AssetView)item.value;
                    RenderTabAsset();
                    RenderAssets();
                });
                num++;
            }
            foreach(var item in assetItems) {
                item.Deselect();
            }
            editor.buildMode = LevelEditor.BuildMode.View;
            assetTabButtons[(int)editor.assetView].Select();
		}

        public void RenderAssets() {
            // Clear Asset View
            foreach(RectTransform child in assetScrollRect.content.transform) {
                Destroy(child.gameObject);
            }
            assetItems = new List<SelectItem>();

            // Render Asset View Tab
            switch(editor.assetView) {
				case LevelEditor.AssetView.Blocks:
					foreach(var block in AssetLoader.GetBlockAssets(editor.theme)) {
						var asset = Instantiate(selectItem.gameObject).GetComponent<SelectItem>();
						asset.transform.SetParent(assetScrollRect.content.transform);
                        asset.SetSprite(block.preview);
                        asset.Deselect();
                        asset.SetOnClick(() => {
                            if(asset.isSelected == false) {
                                editor.buildMode = LevelEditor.BuildMode.Build;
                                editor.selectedBlockType = block.block;
                                RenderAssets();
                            } else {
                                editor.buildMode = LevelEditor.BuildMode.View;
                                asset.Deselect();
                            }
                            UpdateActionButton();
                        });
                        assetItems.Add(asset);
					}
                    if(editor.buildMode == LevelEditor.BuildMode.Build) {
                        assetItems[(int)editor.selectedBlockType].Select();
                    }
                    break;
                case LevelEditor.AssetView.ExtraBlocks:
                    foreach(var block in AssetLoader.GetExtraBlockAssets()) {
                        var asset = Instantiate(selectItem.gameObject).GetComponent<SelectItem>();
                        asset.transform.SetParent(assetScrollRect.content.transform);
                        asset.SetSprite(block.preview);
                        asset.Deselect();
                        asset.SetOnClick(() => {
                            if(asset.isSelected == false) {
                                editor.buildMode = LevelEditor.BuildMode.BuildExtra;
                                editor.selectedExtraBlock = block.block;
                                RenderAssets();
                            } else {
                                editor.buildMode = LevelEditor.BuildMode.View;
                                asset.Deselect();
                            }
                            UpdateActionButton();
                        });
                        assetItems.Add(asset);
                    }
                    if(editor.buildMode == LevelEditor.BuildMode.BuildExtra) {
                        assetItems[GetExtraBlockEnumOrder(editor.selectedExtraBlock)].Select();
                    }
                    break;
                case LevelEditor.AssetView.Flora:
                    foreach (var block in AssetLoader.GetFloraAssets()) {
                        var asset = Instantiate(selectItem.gameObject).GetComponent<SelectItem>();
                        asset.transform.SetParent(assetScrollRect.content.transform);
                        asset.SetSprite(block.preview);
                        asset.Deselect();
                        asset.SetOnClick(() => {
                            if (asset.isSelected == false) {
                                editor.buildMode = LevelEditor.BuildMode.PlaceFlora;
                                editor.selectedFloraBlock = block.block;
                                RenderAssets();
                            } else {
                                editor.buildMode = LevelEditor.BuildMode.View;
                                asset.Deselect();
                            }
                            UpdateActionButton();
                        });
                        assetItems.Add(asset);
                    }
                    if (editor.buildMode == LevelEditor.BuildMode.PlaceFlora) {
                        assetItems[GetFloraBlockEnumOrder(editor.selectedFloraBlock)].Select();
                    }
                    break;
                case LevelEditor.AssetView.Tanks:
					foreach(var tank in AssetLoader.TankAssets.Where(t => t.notSelectable == false)) {
						var asset = Instantiate(selectItem.gameObject).GetComponent<SelectItem>();
						asset.transform.SetParent(assetScrollRect.content.transform);
						asset.SetSprite(tank.preview);
                        asset.Deselect();
                        asset.SetOnClick(() => {
                            if(asset.isSelected == false) {
                                editor.buildMode = LevelEditor.BuildMode.Tanks;
                                editor.selectedTankType = tank.tankType;
                                RenderAssets();
                            } else {
                                editor.buildMode = LevelEditor.BuildMode.View;
                                asset.Deselect();
                            }
                            UpdateActionButton();
                        });
                        assetItems.Add(asset);
                    }
                    if(editor.buildMode == LevelEditor.BuildMode.Tanks) {
                        assetItems[(int)editor.selectedTankType].Select();
                    }
					break;
                case LevelEditor.AssetView.Terrain:
                    foreach(var tile in AssetLoader.GroundTiles.Where(t => t.notSelectable == false)) {
                        var asset = Instantiate(selectItem.gameObject).GetComponent<SelectItem>();
                        asset.transform.SetParent(assetScrollRect.content.transform);
                        asset.value = (int)tile.type;   // Not required but makes debugging easier
                        asset.SetSprite(tile.preview);
                        asset.Deselect();
                        asset.SetOnClick(() => {
                            if(asset.isSelected == false) {
                                if(tile.tileLevel == 0) {
                                    editor.buildMode = LevelEditor.BuildMode.TerraBase;
                                } else if(tile.tileLevel < 0) {
                                    editor.buildMode = LevelEditor.BuildMode.TerraformDown;
								} else {
                                    editor.buildMode = LevelEditor.BuildMode.TerraformUp;
                                }
                                editor.selectedTileType = tile.type;
                                RenderAssets();
                            } else {
                                editor.buildMode = LevelEditor.BuildMode.View;
                                asset.Deselect();
                            }
                            UpdateActionButton();
                        });
                        assetItems.Add(asset);
                    }
                    if(editor.buildMode == LevelEditor.BuildMode.TerraBase || editor.buildMode == LevelEditor.BuildMode.TerraformUp || editor.buildMode == LevelEditor.BuildMode.TerraformDown) {
                        assetItems[(int)editor.selectedTileType].Select();
                    }
                    break;
			}
        }

        public void HideEditorUI() {
            extendedOptionsShown = false;
            extenedOptionsButton.localRotation = Quaternion.Euler(0, 0, 0);
            extendedOptions.DOAnchorPosX(-660, toggleEditorUIDuration);
            assetScrollRect.RectTransform().DOAnchorPosY(-200, toggleEditorUIDuration);
            saveButton.RectTransform().DOAnchorPosX(150, toggleEditorUIDuration);
            actionButton.transform.parent.RectTransform().DOAnchorPosY(-200, toggleEditorUIDuration);
            zoomSlider.RectTransform().DOAnchorPosX(100, toggleEditorUIDuration);
            optionsButton.RectTransform().DOAnchorPosX(100, toggleEditorUIDuration);
        }

        public void ShowEditorUI() {
            extendedOptions.DOAnchorPosX(-475, toggleEditorUIDuration);
            assetScrollRect.RectTransform().DOAnchorPosY(40, toggleEditorUIDuration);
            saveButton.RectTransform().DOAnchorPosX(-50, toggleEditorUIDuration);
            actionButton.transform.parent.RectTransform().DOAnchorPosY(25, toggleEditorUIDuration);
            zoomSlider.RectTransform().DOAnchorPosX(-65, toggleEditorUIDuration);
            optionsButton.RectTransform().DOAnchorPosX(-50, toggleEditorUIDuration);
        }

        public void RefreshUI() {
            if(Application.isPlaying) {
                uiCanvasGroup.gameObject.SetActive(true);
                uiCanvasGroup.alpha = 1;
                editor.DeletePreview();
                editor.buildMode = LevelEditor.BuildMode.View;
                RenderThemeItems();
                RenderGridSizeItems();
                RenderTabAsset();
                RenderAssets();
                RenderAssets();
                UpdateActionButton();
            }
		}

        public int GetExtraBlockEnumOrder(ExtraBlocks type) {
            var values = System.Enum.GetValues(typeof(ExtraBlocks));
            for(int i = 0; i < values.Length; i++) {
                if((ExtraBlocks)values.GetValue(i) == type) {
                    return i;
                }
            }
            return 0;
        }
        public int GetFloraBlockEnumOrder(FloraBlocks type) {
            var values = System.Enum.GetValues(typeof(FloraBlocks));
            for (int i = 0; i < values.Length; i++) {
                if ((FloraBlocks)values.GetValue(i) == type) {
                    return i;
                }
            }
            return 0;
        }
    }
}