using System.Linq;
using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using Newtonsoft.Json;
using Fjord;
using Fjord.Modules.Camera;
using Fjord.Modules.Game;
using Fjord.Modules.Graphics;
using Fjord.Modules.Input;
using Fjord.Modules.Mathf;
using Fjord.Modules.Debug;
using Fjord.Modules.Ui;
using Fjord.Modules.Tilemaps;

namespace Glacier {

    enum scenes {
        Tiles,
        Config,
        Property,
        Tools
    }

    public class main : scene
    {

        string font = "Roboto";

        List<List<Dictionary<string, dynamic>>> tile_map = new List<List<Dictionary<string, dynamic>>>(); 
        Dictionary<string, glacier_tile> tiles = new Dictionary<string, glacier_tile>();

        V2 size = new V2(30, 30);
        V2 tile_size = new V2(16, 16);
        float zoom = 1;

        V2f position = new V2f(0, 0);

        List<glacier_property> properties = new List<glacier_property>() {
            new glacier_property("tile_id", "string", ""),
        };

        V2 hovered = new V2(0, 0);
        V2 selected = new V2(0, 0);
        
        int selected_panel = 0;

        // Add tile vars

        string add_tile_path = "";
        string add_tile_name = "";
        bool add_tile_button = false;

        // Add glacier_property Vars 

        string add_prop_name = "";
        string add_prop_type = "string";
        string add_prop_def_s = "";
        bool add_prop_def_b = false;
        int add_prop_def_i = 0;

        // Tools vars

        string paint_prop_name = "";
        dynamic paint_prop_value = "";

        string highlight_prop_name = "";
        dynamic highlight_prop_value = "";

        // File vars

        string save_path = "";
        string load_path = "";


        public override void on_load()
        {
            for(var i = 0; i < size.x; i++) {
                tile_map.Add(new List<Dictionary<string, dynamic>>());
                for(var j = 0; j < size.y; j++) {
                    tile_map[i].Add(new Dictionary<string, dynamic>() {
                        {"tile_id", ""}
                    });
                }
            }

            game.set_render_background(255, 255, 255, 255);
            input.set_input_state("general");

            draw.load_font(font);

            // This is where you load all your scenes 
            // The if statement is so that it doesn't trigger multiple times

            if(!scene_handler.get_scene("glacier")) {

                // Add all scenes
                scene_handler.add_scene("glacier", new main());

                // Load the first scene this can later be called in any file as for example a win condition to switch scene.
                scene_handler.load_scene("glacier");
            }
        }

        // Update method
        // This is where all your gamelogic is

        public override void update()
        {
            float move_speed = 40f * (float)game.delta_time;
            float zoom_speed = 0.2f * (float)game.delta_time;

            if(input.get_key_pressed(input.key_up, "general")) {
                zoom += zoom < 5 ? zoom_speed : 0;
            } else if(input.get_key_pressed(input.key_down, "general")) {
                zoom -= zoom > 1 ? zoom_speed : 0;
            }

            if(input.get_key_pressed(input.key_w, "general")) {
                position.y -= move_speed;
            } else if(input.get_key_pressed(input.key_s, "general")) {
                position.y += move_speed;
            }

            if(input.get_key_pressed(input.key_a, "general")) {
                position.x -= move_speed;
            } else if(input.get_key_pressed(input.key_d, "general")) {
                position.x += move_speed;
            }

            camera.set(position.x + size.x * tile_size.x / 2 * zoom, position.y + size.y * tile_size.y / 2 * zoom);

            zoom = Math.Clamp(zoom, 1, 5);

            hovered.x = Math.Clamp((mouse.game_position.x + (int)camera.get().x) / (int)(tile_size.x * zoom), 0, size.x);
            hovered.y = Math.Clamp((mouse.game_position.y + (int)camera.get().y) / (int)(tile_size.y * zoom), 0, size.y);

            if(mouse.button_just_pressed(mb.left, "general")) {
                if(helpers.mouse_inside(new V4(0, 0, 95, 50))) {
                    selected_panel = 0;
                } else if(helpers.mouse_inside(new V4(95, 0, 100, 50))) {
                    selected_panel = 1;
                } else if(helpers.mouse_inside(new V4(90, 0, 200, 50))) {
                    selected_panel = 2;
                } else if(helpers.mouse_inside(new V4(200, 0, 200, 50))) {
                    selected_panel = 3;
                }
            }

            if(mouse.button_pressed(mb.left, "general")) {
                if(!helpers.mouse_inside(new V4(0, 0, 400, game.resolution.y))) {
                    if(hovered.x < size.x && hovered.y < size.y && hovered.x > 0 && hovered.y > 0) {
                        tile_map[hovered.x][hovered.y][paint_prop_name] = paint_prop_value;
                    }
                    selected.x = hovered.x;
                    selected.y = hovered.y;
                }
            }

            selected.x = Math.Clamp(selected.x, 0, size.x);
            selected.y = Math.Clamp(selected.y, 0, size.y);

            base.update();
        }

        // Render method
        // This is where all your rendering is

        public override void render()
        {
            draw_tile_map();

            draw_header();
            
            switch(selected_panel) {
                case (int)scenes.Tiles:
                    draw_tiles();
                    break;
                case (int)scenes.Config:
                    draw_config();
                    break;
                case (int)scenes.Property:
                    draw_glacier_property();
                    break;
                case (int)scenes.Tools:
                    draw_tools();
                    break;
            }

            if(input.get_input_state() == "add_tile") {
                draw_add_tile();
            } else if(input.get_input_state() == "add_glacier_property") {
                draw_add_glacier_property();
            } else if(input.get_input_state() == "file") {
                draw_file();
            }

            // draw.text(new V2(0, 0), font, 24, "x: " + selected.x.ToString() + " y: " + selected.y.ToString(), color.black);

            base.render();
        }

        private void draw_tile_map() {
            for(var i = 0; i < size.x; i++) {
                for(var j = 0; j < size.y; j++) {
                    V2 pos = new V2((int)(i * (tile_size.x * zoom) - camera.get().x), (int)(j * (tile_size.y * zoom) - camera.get().y));
                    V2 size = new V2((int)(tile_size.x * zoom), (int)(tile_size.y * zoom));
                    draw.rect(new V4(pos.x, pos.y, size.x, size.y), color.black, false);
                    if(tiles.Keys.ToList().Contains(tile_map[i][j]["tile_id"])) {
                        texture tmp =  (texture)tiles[tile_map[i][j]["tile_id"]].tex.Clone();
                        float size_ =(zoom / 3.2f + 0.6875f); 
                        tmp.set_scale(new V2f(size_, size_));
                        draw.texture_direct(pos, tmp);
                    }
                    if(selected.x == i && selected.y == j) {
                        draw.rect(new V4(pos.x, pos.y, size.x, size.y), new V4(214, 229, 250, 170));   
                    } else if(hovered.x == i && hovered.y == j) {
                        draw.rect(new V4(pos.x, pos.y, size.x, size.y), new V4(163, 218, 141, 170));
                    }

                    bool contains = false;
                    glacier_property prop = properties[0];

                    foreach(glacier_property x in properties) {
                        if(x.name == highlight_prop_name) {
                            contains = true;
                            prop = x;
                            break;
                        }
                    }

                    if(!tile_map[i][j].Keys.ToArray().Contains(highlight_prop_name) && contains) {
                        tile_map[i][j][highlight_prop_name] = prop.default_value;
                    }

                    if(tile_map[i][j].Keys.ToArray().Contains(highlight_prop_name)) {
                        if(tile_map[i][j][highlight_prop_name] == highlight_prop_value) {
                            if(contains) {
                                draw.rect(new V4(pos.x, pos.y, size.x, size.y), new V4(255, 89, 89, 170));
                            }
                        }
                    }
                }
            }
        }

        private void draw_header() {
            draw.rect(new V4(0, 0, 400, game.resolution.y), new V4(250, 250, 250, 255));
            draw.rect(new V4(400, 0, 4, game.resolution.y), new V4(245, 245, 245, 255));

            List<int> widths = new List<int>();
            int iter = 0;
            foreach(var scene in Enum.GetValues(typeof(scenes))) {
                string? stringed = scene.ToString();
                int loc_width = 30;
                foreach(int width in widths) {
                    loc_width += width;
                }
                draw.text(new V2(loc_width, 20), "Roboto-Bold", 20, stringed, selected_panel == iter ? new V4(236, 72, 112, 255) : new V4(180, 180, 190, 255));
                widths.Add(draw.get_text_rect(new V2(0, 0), "Roboto-Bold", 20, stringed).z + 30);
            
                if(iter == selected_panel) {
                    draw.rect(new V4(loc_width - 5, -4, draw.get_text_rect(new V2(0, 0), "Roboto-Bold", 20, stringed).z + 10, 10), new V4(236, 72, 112, 255), true, 6);
                }

                iter++;
            } 
        }
    
        private void draw_tiles() {
            V4 add_button_rect = new V4(30, 70, 150, 50);

            draw.rect(add_button_rect, !helpers.mouse_inside(add_button_rect) ? new V4(90, 45, 230, 255) : new V4(170, 150, 240, 255), true, 25);
            draw.text(new V2(65, 84), "Roboto-Bold", 20, "Add Tile");
            if(helpers.mouse_inside(add_button_rect) && mouse.button_just_pressed(mb.left, "general")) {
                input.set_input_state("add_tile");
            }

            for(var i = 0; i < tiles.Count; i++) {
                texture tmp = (texture)tiles[tiles.Keys.ToList()[i]].tex.Clone();
                int size = 64 / tile_size.y / 2;
                tmp.set_scale(new V2f(size, size));
                draw.texture(new V2(30, 150 + i * tile_size.y * size), tmp);
                draw.text(new V2(100, 150 + i * tile_size.y * size), font, 24, tiles[tiles.Keys.ToList()[i]].name, color.black);
                draw.text(new V2(100, 190 + i * tile_size.y * size), font, 24, tiles[tiles.Keys.ToList()[i]].path, color.black);
            }
        }

        private void draw_add_tile() {
            draw.rect(new V4(0, 0, game.resolution.x, game.resolution.y), new V4(0, 0, 0, 170), true);
            V4 panel_rect = new V4(game.resolution.x / 2 - 200, game.resolution.y / 2 - 300, 400, 600);
            draw.rect(panel_rect, color.white, true, 25);
        
            devgui.input_box(new V4(20 + panel_rect.x, 75 + panel_rect.y, 250, 35), font, ref add_tile_path, null, "add_tile_tex", "Texture Path");
            devgui.input_box(new V4(20 + panel_rect.x, 20 + panel_rect.y, 250, 35), font, ref add_tile_name, null, "add_tile_name", "Name");

            devgui.button(new V4(20 + panel_rect.x, 145 + panel_rect.y, 80, 35), ref add_tile_button, "Roboto-Bold", "Add");

            if(add_tile_button) {
                tiles.Add(add_tile_name, new glacier_tile(add_tile_name, add_tile_path));
                add_tile_name = "";
                add_tile_path = "";
                add_tile_button = false;
                input.set_input_state("general");
            }

            if(input.get_key_just_pressed(input.key_escape)) {
                add_tile_name = "";
                add_tile_path = "";
                add_tile_button = false;
                input.set_input_state("general");
            }

            if(input.get_key_just_pressed(input.key_return)) {
                tiles.Add(add_tile_name, new glacier_tile(add_tile_name, add_tile_path));
                add_tile_name = "";
                add_tile_path = "";
                add_tile_button = false;
                input.set_input_state("general");
            }
        }
    
        private void draw_config() {
            for(var i = 0; i < properties.Count; i++) {
                if(!tile_map[selected.x][selected.y].Keys.ToArray().Contains(properties[i].name)) {
                    tile_map[selected.x][selected.y].Add(properties[i].name, properties[i].default_value);
                }

                int offset = 130 * i;

                draw.text(new V2(30, 70 + offset), font, 24, properties[i].name, color.black); 
                draw.rect(new V4(40 + draw.get_text_rect(new V2(30, 70), font, 24, properties[i].name).z, 70 + offset, 30, 30), new V4(103, 111, 163, 255), true, 5);
                draw.text(new V2(47 + draw.get_text_rect(new V2(30, 70), font, 24, properties[i].name).z, 71 + offset), "Roboto-Bold", 24, properties[i].datatype.ToString()[0].ToString().ToUpper());  
            
                if(properties[i].datatype == "string") {
                    string tmp = tile_map[selected.x][selected.y][properties[i].name];
                    devgui.input_box(new V4(30, 110 + offset, 200, 35), font, ref tmp, "general", properties[i].name, "");
                    tile_map[selected.x][selected.y][properties[i].name] = tmp;
                } else if(properties[i].datatype == "int") {
                    int tmp = tile_map[selected.x][selected.y][properties[i].name];
                    devgui.num_input_box(new V4(30, 110 + offset, 200, 35), font, ref tmp, "general", properties[i].name);
                    tile_map[selected.x][selected.y][properties[i].name] = tmp;
                } else if(properties[i].datatype == "bool") {
                    bool tmp = tile_map[selected.x][selected.y][properties[i].name];
                    devgui.button(new V4(30, 110 + offset, draw.get_text_rect(new V2(0, 0), font, 24, properties[i].name).z + 10, 35), ref tmp, font, "x");
                    tile_map[selected.x][selected.y][properties[i].name] = tmp;
                }
            }
        }

        private void draw_glacier_property() {
            V4 add_button_rect = new V4(30, 70, 200, 50);

            draw.rect(add_button_rect, !helpers.mouse_inside(add_button_rect) ? new V4(90, 45, 230, 255) : new V4(170, 150, 240, 255), true, 25);
            draw.text(new V2(65, 84), "Roboto-Bold", 20, "Add Property");
            if(helpers.mouse_inside(add_button_rect) && mouse.button_just_pressed(mb.left, "general")) {
                input.set_input_state("add_glacier_property");
            } 

            for(var i = 0; i < properties.Count; i++) {
                int offset = 80 + 80 * i;
                
                draw.text(new V2(30, 70 + offset), font, 24, properties[i].name, color.black); 
                draw.rect(new V4(40 + draw.get_text_rect(new V2(30, 70), font, 24, properties[i].name).z, 70 + offset, 30, 30), new V4(103, 111, 163, 255), true, 5);
                draw.text(new V2(47 + draw.get_text_rect(new V2(30, 70), font, 24, properties[i].name).z, 71 + offset), "Roboto-Bold", 24, properties[i].datatype.ToString()[0].ToString().ToUpper());  
            
                if(properties[i].datatype == "string") {
                    glacier_property tmp = properties[i];
                    string tmp_ = tmp.default_value;
                    devgui.input_box(new V4(30, 110 + offset, 200, 35), font, ref tmp_, "general", properties[i].name, "");
                    tmp.default_value = tmp_;
                    properties[i] = tmp;
                } else if(properties[i].datatype == "int") {
                    glacier_property tmp = properties[i];
                    int tmp_ = tmp.default_value;
                    devgui.num_input_box(new V4(30, 110 + offset, 200, 35), font, ref tmp_, "general", properties[i].name);
                    tmp.default_value = tmp_;
                    properties[i] = tmp;
                } else if(properties[i].datatype == "bool") {
                    glacier_property tmp = properties[i];
                    bool tmp_ = tmp.default_value;
                    devgui.button(new V4(30, 110 + offset, draw.get_text_rect(new V2(0, 0), font, 24, properties[i].name).z + 10, 35), ref tmp_, font, "x");
                    tmp.default_value = tmp_;
                    properties[i] = tmp;
                }
            }
        }

        private void draw_add_glacier_property() {
            draw.rect(new V4(0, 0, game.resolution.x, game.resolution.y), new V4(0, 0, 0, 170), true);
            V4 panel_rect = new V4(game.resolution.x / 2 - 200, game.resolution.y / 2 - 300, 400, 600);
            draw.rect(panel_rect, color.white, true, 25);

            draw.text(new V2(20 + panel_rect.x, 20 + panel_rect.y), "Roboto", 24, "Name", color.black);
            devgui.input_box(new V4(20 + panel_rect.x, 70 + panel_rect.y, 200, 35), "Roboto", ref add_prop_name, "add_glacier_property", "add_prop_name", "");
    
            bool string_ = false;
            bool int_ = false;
            bool bool_ = false;

            draw.text(new V2(20 + panel_rect.x, 130 + panel_rect.y), "Roboto", 24, "Type", color.black);
            
            draw.rect(new V4(80 + panel_rect.x, 130 + panel_rect.y, 30, 30), new V4(103, 111, 163, 255), true, 5);
            draw.text(new V2(87 + panel_rect.x, 131 + panel_rect.y), "Roboto-Bold", 24, add_prop_type.ToString()[0].ToString().ToUpper());  

            devgui.button(new V4(20 + panel_rect.x, 175 + panel_rect.y, 80, 35), ref string_, "Roboto", "String");
            devgui.button(new V4(120 + panel_rect.x, 175 + panel_rect.y, 80, 35), ref bool_, "Roboto", "Bool");
            devgui.button(new V4(220 + panel_rect.x, 175 + panel_rect.y, 80, 35), ref int_, "Roboto", "Int");

            if(string_) {
                add_prop_type = "string";
            } else if(bool_) {
                add_prop_type = "bool";
            } else if(int_) {
                add_prop_type = "int";
            }

            draw.text(new V2(20 + panel_rect.x, 285 + panel_rect.y), "Roboto", 24, "Default", color.black);

            if(add_prop_type == "string") {
                devgui.input_box(new V4(20 + panel_rect.x, 330 + panel_rect.y, 300, 35), "Roboto", ref add_prop_def_s, "add_glacier_property", "add_prop_def", "");
            } else if(add_prop_type == "int") {
                devgui.num_input_box(new V4(20 + panel_rect.x, 330 + panel_rect.y, 300, 35), "Roboto", ref add_prop_def_i, "add_glacier_property", "add_prop_def");
            } else if(add_prop_type == "bool") {
                devgui.button(new V4(20 + panel_rect.x, 330 + panel_rect.y, 110, 35), ref add_prop_def_b, "Roboto", "Default");
            }

            bool add = false;
            devgui.button(new V4(20 + panel_rect.x, 385 + panel_rect.y, 80, 35), ref add, "Roboto", "Add");

            if(add || input.get_key_just_pressed(input.key_return)) {
                if(add_prop_name != "") {
                    properties.Add(new glacier_property(add_prop_name, add_prop_type, add_prop_type == "string" ? add_prop_def_s : add_prop_type == "bool" ? add_prop_def_b : add_prop_type == "int" ? add_prop_def_i : ""));
                    
                    add_prop_name = "";
                    add_prop_def_b = false;
                    add_prop_def_i = 0;
                    add_prop_def_s = "";
                    add_prop_type = "string";

                    input.set_input_state("general");
                }
            }
        }
    
        private void draw_tools() {
            draw.text(new V2(30, 70), font, 24, "Paint", color.black);
            
            string tmp_ = paint_prop_name;
            devgui.input_box(new V4(30, 120, 200, 35), font, ref paint_prop_name, "general", "paint_prop_name", "");

            foreach(glacier_property i in properties) {
                if(i.name == paint_prop_name) {
                    if(i.datatype == "string") {
                        if(paint_prop_name != tmp_) {
                            paint_prop_value = "";
                        }

                        string tmp = paint_prop_value;
                        devgui.input_box(new V4(30, 175, 300, 35), font, ref tmp, "general", "paint_prop_value", "");
                        paint_prop_value = tmp;
                    } else if(i.datatype == "int") {
                        if(paint_prop_name != tmp_) {
                            paint_prop_value = 0;
                        }

                        int tmp = paint_prop_value;
                        devgui.num_input_box(new V4(30, 175, 300, 35), font, ref tmp, "general", "paint_prop_value");
                        paint_prop_value = tmp;
                    } else if(i.datatype == "bool") {
                        if(paint_prop_name != tmp_) {
                            paint_prop_value = false;
                        }

                        bool tmp = paint_prop_value;
                        devgui.button(new V4(30, 175, 110, 35), ref tmp, font, "x");
                        paint_prop_value = tmp;
                    }
                    break;
                }
            }

            draw.text(new V2(30, 250), font, 24, "Highlight", color.black);

            tmp_ = highlight_prop_name;
            devgui.input_box(new V4(30, 300, 200, 35), font, ref highlight_prop_name, "general", "highlight_prop_name", "");

            foreach(glacier_property i in properties) {
                if(i.name == highlight_prop_name) {
                    if(i.datatype == "string") {
                        if(highlight_prop_name != tmp_) {
                            highlight_prop_value = "";
                        }

                        string tmp = highlight_prop_value;
                        devgui.input_box(new V4(30, 355, 300, 35), font, ref tmp, "general", "highlight_prop_value", "");
                        highlight_prop_value = tmp;
                    } else if(i.datatype == "int") {
                        if(highlight_prop_name != tmp_) {
                            highlight_prop_value = 0;
                        }

                        int tmp = highlight_prop_value;
                        devgui.num_input_box(new V4(30, 355, 300, 35), font, ref tmp, "general", "highlight_prop_value");
                        highlight_prop_value = tmp;
                    } else if(i.datatype == "bool") {
                        if(highlight_prop_name != tmp_) {
                            highlight_prop_value = false;
                        }

                        bool tmp = highlight_prop_value;
                        devgui.button(new V4(30, 355, 110, 35), ref tmp, font, "x");
                        highlight_prop_value = tmp;
                    }
                    break;
                }
            }

            V4 file_button_rect = new V4(30, game.resolution.y - 80, 100, 50);

            draw.rect(file_button_rect, !helpers.mouse_inside(file_button_rect) ? new V4(90, 45, 230, 255) : new V4(170, 150, 240, 255), true, 25);
            draw.text(new V2(65, game.resolution.y - 66), "Roboto-Bold", 20, "File");
            if(helpers.mouse_inside(file_button_rect) && mouse.button_just_pressed(mb.left, "general")) {
                input.set_input_state("file");
            }
        }   
    
        private void draw_file() {
            draw.rect(new V4(0, 0, game.resolution.x, game.resolution.y), new V4(0, 0, 0, 170), true);
            V4 panel_rect = new V4(game.resolution.x / 2 - 200, game.resolution.y / 2 - 300, 400, 600);
            draw.rect(panel_rect, color.white, true, 25);

            bool save_ = false;
            draw.text(new V2(30 + panel_rect.x, 30 + panel_rect.y), font, 24, "Save", color.black);
            devgui.input_box(new V4(30 + panel_rect.x, 60 + panel_rect.y, 300, 35), font, ref save_path, "file", "save_file", "");
            devgui.button(new V4(30 + panel_rect.x, 125 + panel_rect.y, 80, 35), ref save_, font, "Save");
            if(save_) {
                save(save_path);
                input.set_input_state("general");
            }

            bool load_ = false;
            draw.text(new V2(30 + panel_rect.x, 180 + panel_rect.y), font, 24, "Load", color.black);
            devgui.input_box(new V4(30 + panel_rect.x, 210 + panel_rect.y, 300, 35), font, ref load_path, "file", "load_file", "");
            devgui.button(new V4(30 + panel_rect.x, 275 + panel_rect.y, 80, 35), ref load_, font, "Load");
            if(load_) {
                load(load_path);
                input.set_input_state("general");
            }
        }
    
        private void save(string path) {
            glacier_format format = new glacier_format();
            format.grid_size = size;
            format.tile_size = tile_size;
            format.properties = properties;
            format.tile_map = tile_map;
            format.tiles = tiles;

            string jsonString = JsonConvert.SerializeObject(format);
            Debug.send(jsonString);
            if(!Directory.Exists(game.get_resource_folder() + "/" + game.asset_pack + "/data")) {
                Directory.CreateDirectory(game.get_resource_folder() + "/" + game.asset_pack + "/data");
            }

            if(!Directory.Exists(game.get_resource_folder() + "/" + game.asset_pack + "/data/tilemaps")) {
                Directory.CreateDirectory(game.get_resource_folder() + "/" + game.asset_pack + "/data/tilemaps");
            }

            File.WriteAllText(game.get_resource_folder() + "/" + game.asset_pack + "/data/tilemaps/" + path + ".glacier", jsonString);
        }

        private void load(string path) {
            string JsonString = "";
            if(File.Exists(game.get_resource_folder() + "/" + game.asset_pack + "/data/tilemaps/" + path + ".glacier")) {
                JsonString = File.ReadAllText(game.get_resource_folder() + "/" + game.asset_pack + "/data/tilemaps/" + path + ".glacier");
            } else {
                return;
            }

            glacier_format? format = JsonConvert.DeserializeObject<glacier_format>(JsonString);

            foreach(string key in format.tiles.Keys) {
                format.tiles[key].tex.set_texture(format.tiles[key].path);
            }
            
            size = format.grid_size;
            tile_size = format.tile_size;
            properties = format.properties;
            tile_map = format.tile_map;
            tiles = format.tiles;
        }
    }

    // Main Class

    class Program 
    {
        public static void Main(string[] args) 
        {
            // Function that starts game
            // The parameter should be your start scene
            game.set_resource_folder("resources");
            game.run(new main());
        }
    }
}