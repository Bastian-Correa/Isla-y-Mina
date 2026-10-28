# Isla y mina procedurales

Tarea 2 de IA y PCG para Videojuegos (Bastian Correa Díaz).

Software en Unity que genera dos escenarios de forma procedimental: una isla y una mina abandonada. Cada escenario tiene un menú para cambiar la semilla y los parámetros, y se vuelve a generar al soltar el mouse.

**Probar en el navegador:** https://bastiancorreautalca.itch.io/isla-y-mina

## Técnicas usadas

| Familia | Técnica | Dónde se usa |
|---|---|---|
| Paisajes | Ruido Perlin con octavas (fBm) | Terreno de la isla y zonas de bosque |
| Secuencias | L-Systems estocásticos + tortuga 3D | Árboles de la isla y cristales de la mina |
| Mazmorras | BSP (Binary Space Partitioning) | Salas y pasillos de la mina |

También se usa BFS para medir distancias dentro de la mina y poner los cristales en las salas más lejanas.

## Cómo abrirlo

1. Instalar Unity 6 desde Unity Hub.
2. Clonar o descargar este repositorio.
3. En Unity Hub: **Add > Add project from disk** y elegir la carpeta del repositorio.
4. Abrir la escena en `Assets/Scenes` y presionar Play.

El proyecto usa el sistema de input antiguo (`Active Input Handling = Both`), que ya viene configurado en `ProjectSettings`.

## Controles

- Clic derecho + arrastrar: rotar la cámara
- Rueda del mouse: zoom
- WASD o flechas: mover
- T: vista desde arriba
- N: semilla aleatoria
- H: ocultar o mostrar el menú
- Botones **Isla** y **Mina** (arriba): cambiar de escenario

## Scripts

Todos están en `Assets/Scripts`:

- `Isla.cs`: escenario de la isla, menú y panel.
- `GeneradorTerreno.cs`: alturas con ruido Perlin, malla y textura.
- `LSistema.cs`: reglas de cada especie y reescritura del L-System.
- `Tortuga.cs`: arma la malla 3D a partir de la cadena del L-System.
- `Mina.cs`: escenario de la mina, objetos, luces, menú y panel.
- `GeneradorMina.cs`: BSP, pasillos y BFS.
- `CamaraOrbital.cs`: cámara.
- `SelectorContexto.cs`: botones para cambiar entre isla y mina.

