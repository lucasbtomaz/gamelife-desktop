import { describe, expect, it } from 'vitest';
import { filtrarDesejos } from './wishlist';

describe('filtro da lista de desejos', () => {
  it('encontra o mesmo jogo sem marcas, pontuação ou acentos', () => {
    const itens = [
      { titulo: "Sid Meier's Civilization® VII" },
      { titulo: 'Pokémon Legends' }
    ];

    expect(filtrarDesejos(itens, 'Sid Meiers Civilization VII').map((item) => item.titulo))
      .toEqual(["Sid Meier's Civilization® VII"]);
    expect(filtrarDesejos(itens, 'pokemon').map((item) => item.titulo))
      .toEqual(['Pokémon Legends']);
  });
});
